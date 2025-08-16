using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MimeKit;
using Microsoft.EntityFrameworkCore;
using OfflineEmailManager.Data;
using OfflineEmailManager.Model;
using System.ComponentModel;
using System.Windows.Data;
using System.IO;
using WF = System.Windows.Forms; // FolderBrowserDialog
using Win32OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Win32SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace OfflineEmailManager;

using Microsoft.EntityFrameworkCore;


public partial class MainWindow : Window
{
    private readonly EmailDbContext _context = new EmailDbContext();

    public MainWindow()
    {
        InitializeComponent();
        _context.Database.EnsureCreated();
        LoadEmailsFromDb();
    }

    private void LoadEmailsFromDb()
    {
        // Implementation from the previous step...
        var all = _context.Emails.ToList();
        EmailListBox.ItemsSource = all;
        UpdateStatus(all.Count, "All (no filter)");
        // Reapply last sort, if any
        if (!string.IsNullOrEmpty(_lastSortProperty))
        {
            ApplySort(_lastSortProperty!, _lastDirection, updateHeader: true);
        }
    }


    private void LoadFiles_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Win32OpenFileDialog
        {
            Multiselect = true,
            Filter = "Email files (*.eml)|*.eml"
        };

        if (openFileDialog.ShowDialog() != true) return;

        int loadedCount = 0;
        int overwrittenCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        bool? overwriteAll = null; // null = ask, true = overwrite all, false = skip all

        // Optional performance hint when importing many files:
        // var originalDetect = _context.ChangeTracker.AutoDetectChangesEnabled;
        // _context.ChangeTracker.AutoDetectChangesEnabled = false;

        foreach (var filePath in openFileDialog.FileNames)
        {
            try
            {
                var message = MimeMessage.Load(filePath);
                var subject = (message.Subject ?? "(no subject)").Trim();

                var existing = _context.Emails.FirstOrDefault(e => e.Subject == subject);
                if (existing != null)
                {
                    if (!ShouldOverwrite(subject, ref overwriteAll))
                    {
                        skippedCount++;
                        continue;
                    }

                    ApplyTo(existing, message);
                    overwrittenCount++;
                }
                else
                {
                    var newEmail = CreateFrom(message);
                    _context.Emails.Add(newEmail);
                    loadedCount++;
                }
            }
            catch
            {
                // Log if you have logging; for now just count failures
                failedCount++;
            }
        }

        _context.SaveChanges();

        // Optional: restore if you toggled AutoDetectChangesEnabled
        // _context.ChangeTracker.AutoDetectChangesEnabled = originalDetect;

        // Build a concise summary
        var summary = $"Imported: {loadedCount}, Overwritten: {overwrittenCount}, Skipped: {skippedCount}";
        if (failedCount > 0) summary += $", Failed: {failedCount}";
        System.Windows.MessageBox.Show(summary);

        LoadEmailsFromDb();

        // Local helpers keep scope tight and avoid class bloat
        static Email CreateFrom(MimeMessage m)
        {
            var email = new Email
            {
                Subject = (m.Subject ?? "(no subject)").Trim(),
                From = string.Join(",", m.From.Select(a =>
                    (a as MailboxAddress)?.Name ?? (a as MailboxAddress)?.Address ?? a.ToString())),
                Date = m.Date.DateTime,
                BodyHtml = m.HtmlBody,
                BodyText = m.TextBody,
                Attachments = new List<Attachment>()
            };

            foreach (var part in m.Attachments)
            {
                if (part is MimePart mp)
                {
                    using var ms = new MemoryStream();
                    mp.Content.DecodeTo(ms);
                    email.Attachments.Add(new Attachment
                    {
                        FileName = mp.FileName ?? "attachment",
                        ContentType = mp.ContentType.MimeType,
                        Size = ms.Length,
                        Data = ms.ToArray()
                    });
                }
                else if (part is MessagePart rfc822)
                {
                    using var ms = new MemoryStream();
                    rfc822.Message.WriteTo(ms);
                    email.Attachments.Add(new Attachment
                    {
                        FileName = (rfc822.Message.Subject ?? "attached-message") + ".eml",
                        ContentType = "message/rfc822",
                        Size = ms.Length,
                        Data = ms.ToArray()
                    });
                }
            }
            return email;
        }

        static void ApplyTo(Email target, MimeMessage m)
        {
            target.From = string.Join(",", m.From.Select(a =>
                (a as MailboxAddress)?.Name ?? (a as MailboxAddress)?.Address ?? a.ToString()));
            target.Date = m.Date.DateTime;
            target.BodyHtml = m.HtmlBody;
            target.BodyText = m.TextBody;

            // Replace attachments completely for overwrite scenario
            target.Attachments.Clear();
            foreach (var part in m.Attachments)
            {
                if (part is MimePart mp)
                {
                    using var ms = new MemoryStream();
                    mp.Content.DecodeTo(ms);
                    target.Attachments.Add(new Attachment
                    {
                        FileName = mp.FileName ?? "attachment",
                        ContentType = mp.ContentType.MimeType,
                        Size = ms.Length,
                        Data = ms.ToArray()
                    });
                }
                else if (part is MessagePart rfc822)
                {
                    using var ms = new MemoryStream();
                    rfc822.Message.WriteTo(ms);
                    target.Attachments.Add(new Attachment
                    {
                        FileName = (rfc822.Message.Subject ?? "attached-message") + ".eml",
                        ContentType = "message/rfc822",
                        Size = ms.Length,
                        Data = ms.ToArray()
                    });
                }
            }
        }

        static bool ShouldOverwrite(string subject, ref bool? overwriteAll)
        {
            if (overwriteAll.HasValue) return overwriteAll.Value;

            var result = System.Windows.MessageBox.Show(
                $"Email with subject '{subject}' already exists.\n" +
                "Do you want to overwrite it?\n" +
                "Yes = Overwrite, No = Skip, Cancel = Choose action for all subsequent matches.",
                "Email already exists",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                var allResult = System.Windows.MessageBox.Show(
                    "Apply this action to all subsequent matches?\nYes = Overwrite All, No = Skip All",
                    "Apply to All",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                overwriteAll = (allResult == MessageBoxResult.Yes);
                return overwriteAll.Value;
            }

            return result == MessageBoxResult.Yes;
        }
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var query = SearchTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            // Reset to all emails if query is empty
            LoadEmailsFromDb();
            return;
        }

        var emails = _context.Emails.AsQueryable();
        // Use EF.Functions.Like for database-side contains (case-insensitive depends on collation; SQLite default is case-insensitive for ASCII)
        string pattern = $"%{query}%";

        if (SearchSubjectRadio.IsChecked == true)
        {
            var result = emails
                .Where(e => EF.Functions.Like(e.Subject ?? string.Empty, pattern))
                .ToList();
            EmailListBox.ItemsSource = result;
            UpdateStatus(result.Count, $"Subject contains '{query}'");
            if (!string.IsNullOrEmpty(_lastSortProperty))
            {
                ApplySort(_lastSortProperty!, _lastDirection, updateHeader: true);
            }
        }
        else // Subject + Body
        {
            var result = emails
                .Where(e => EF.Functions.Like(e.Subject ?? string.Empty, pattern)
                         || EF.Functions.Like(e.BodyText ?? string.Empty, pattern)
                         || EF.Functions.Like(e.BodyHtml ?? string.Empty, pattern))
                .ToList();
            EmailListBox.ItemsSource = result;
            UpdateStatus(result.Count, $"Subject+Body contains '{query}'");
            if (!string.IsNullOrEmpty(_lastSortProperty))
            {
                ApplySort(_lastSortProperty!, _lastDirection, updateHeader: true);
            }
        }
    }

    private void SearchClearButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = string.Empty;
        LoadEmailsFromDb();
    }

    private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            SearchButton_Click(sender!, e);
            e.Handled = true;
        }
    }

    private void UpdateStatus(int count, string filterDescription)
    {
        if (StatusTextBlock != null)
        {
            StatusTextBlock.Text = $"Results: {count} | Filter: {filterDescription}";
        }
    }
    private void EmailListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EmailListBox.SelectedItem is Email selectedEmail)
        {
            // Show body
            if (!string.IsNullOrEmpty(selectedEmail.BodyHtml))
            {
                EnsureWebView2(() => EmailContentViewer.CoreWebView2.NavigateToString(selectedEmail.BodyHtml));
            }
            else
            {
                var safeText = selectedEmail.BodyText ?? string.Empty;
                EnsureWebView2(() => EmailContentViewer.CoreWebView2.NavigateToString($"<pre>{System.Net.WebUtility.HtmlEncode(safeText)}</pre>"));
            }

            // Populate attachments list
            if (selectedEmail.Attachments != null && selectedEmail.Attachments.Any())
            {
                AttachmentList.ItemsSource = selectedEmail.Attachments
                    .Select(a => new AttachmentView(a))
                    .ToList();
                AttachmentGroup.Visibility = Visibility.Visible;
            }
            else
            {
                AttachmentList.ItemsSource = null;
                AttachmentGroup.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            AttachmentList.ItemsSource = null;
            AttachmentGroup.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveSelectedAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (AttachmentList.SelectedItem is AttachmentView av)
        {
            SaveAttachmentToDisk(av);
        }
    }

    private void SaveAllAttachments_Click(object sender, RoutedEventArgs e)
    {
        if (AttachmentList.Items.Count == 0) return;
        var folder = SelectFolder();
        if (folder == null) return;
        foreach (AttachmentView av in AttachmentList.Items)
        {
            WriteAttachmentFile(av, folder);
        }
        System.Windows.MessageBox.Show("All attachments saved.");
    }

    private void AttachmentList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (AttachmentList.SelectedItem is AttachmentView av)
        {
            SaveAttachmentToDisk(av);
        }
    }

    private void SaveAttachmentToDisk(AttachmentView av)
    {
        var sfd = new Win32SaveFileDialog
        {
            FileName = av.FileName,
            Filter = "All Files (*.*)|*.*"
        };
        if (sfd.ShowDialog() == true)
        {
            File.WriteAllBytes(sfd.FileName, av.Data);
            System.Windows.MessageBox.Show("Attachment saved.");
        }
    }

    private string? SelectFolder()
    {
        using var fbd = new WF.FolderBrowserDialog();
        var result = fbd.ShowDialog();
        if (result == WF.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            return fbd.SelectedPath;
        }
        return null;
    }

    private void WriteAttachmentFile(AttachmentView av, string folder)
    {
        try
        {
            var targetPath = System.IO.Path.Combine(folder, av.FileName);
            File.WriteAllBytes(targetPath, av.Data);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to save {av.FileName}: {ex.Message}");
        }
    }

    private record AttachmentView(int Id, string FileName, string ContentType, long Size, byte[] Data)
    {
        public AttachmentView(Attachment a) : this(a.Id, a.FileName, a.ContentType, a.Size, a.Data) { }
    }

    private async void EnsureWebView2(System.Action action)
    {
        if (EmailContentViewer.CoreWebView2 == null)
        {
            await EmailContentViewer.EnsureCoreWebView2Async();
        }
        action();
    }

    private GridViewColumnHeader? _lastHeaderClicked;
    private ListSortDirection _lastDirection = ListSortDirection.Ascending;
    private string? _lastSortProperty;

    private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not GridViewColumnHeader header || header.Tag is not string propertyName || string.IsNullOrWhiteSpace(propertyName))
            return;

        var collectionView = CollectionViewSource.GetDefaultView(EmailListBox.ItemsSource);
        if (collectionView == null) return;

        var direction = ListSortDirection.Ascending;
        if (_lastSortProperty == propertyName && _lastDirection == ListSortDirection.Ascending)
        {
            direction = ListSortDirection.Descending;
        }

        _lastHeaderClicked = header;
        _lastDirection = direction;
        _lastSortProperty = propertyName;

        ApplySort(propertyName, direction, updateHeader: true);
    }

    private void ApplySort(string propertyName, ListSortDirection direction, bool updateHeader)
    {
        var collectionView = CollectionViewSource.GetDefaultView(EmailListBox.ItemsSource);
        if (collectionView == null) return;
        collectionView.SortDescriptions.Clear();
        collectionView.SortDescriptions.Add(new SortDescription(propertyName, direction));
        collectionView.Refresh();

        if (updateHeader)
        {
            UpdateHeaderGlyphs(propertyName, direction);
        }
    }

    private void CopySubject_Click(object sender, RoutedEventArgs e)
    {
        if (EmailListBox.SelectedItem is Email selected)
        {
            System.Windows.Clipboard.SetText(selected.Subject ?? string.Empty);
            UpdateStatus(((System.Collections.ICollection)EmailListBox.ItemsSource).Count, "Copied subject to clipboard");
        }
    }

    private void CopyFrom_Click(object sender, RoutedEventArgs e)
    {
        if (EmailListBox.SelectedItem is Email selected)
        {
            System.Windows.Clipboard.SetText(selected.From ?? string.Empty);
            UpdateStatus(((System.Collections.ICollection)EmailListBox.ItemsSource).Count, "Copied sender to clipboard");
        }
    }

    private void DeleteEmail_Click(object sender, RoutedEventArgs e)
    {
        if (EmailListBox.SelectedItem is not Email selected) return;
        var confirm = System.Windows.MessageBox.Show($"Delete email '{selected.Subject}'?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        _context.Emails.Remove(selected);
        _context.SaveChanges();

        // Refresh current view respecting existing filter
        if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
        {
            LoadEmailsFromDb();
        }
        else
        {
            SearchButton_Click(sender, new RoutedEventArgs());
        }
    }

    private void UpdateHeaderGlyphs(string propertyName, ListSortDirection direction)
    {
        if (EmailListBox.View is not GridView gv) return;
        foreach (var col in gv.Columns)
        {
            if (col.Header is GridViewColumnHeader h)
            {
                var baseText = h.Content?.ToString() ?? string.Empty;
                baseText = baseText.Replace(" ▲", string.Empty).Replace(" ▼", string.Empty);
                if ((h.Tag as string) == propertyName)
                {
                    var glyph = direction == ListSortDirection.Ascending ? " ▲" : " ▼";
                    h.Content = baseText + glyph;
                }
                else
                {
                    h.Content = baseText;
                }
            }
        }
    }
}
