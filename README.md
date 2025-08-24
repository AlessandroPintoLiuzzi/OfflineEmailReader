# OfflineEmailManager

A simple offline Windows app to import, store, and browse .eml emails locally. It uses SQLite for storage, Entity Framework Core for data access, and WebView2 to render HTML emails.

## Features

- Import .eml files from disk (multi-select)
- Duplicate handling: Skip or Overwrite, with "apply to all" option
- Stores emails in a local SQLite database (`local_emails.db`)
- Browse emails in a list with Subject, From, and Date
- Modern HTML rendering via WebView2; plain-text emails are shown as `<pre>`
- **Full attachment support**
  - Automatic extraction and storage of email attachments during import
  - Support for both file attachments and embedded RFC822 messages
  - View attachment list with filename, content type, and size information
  - Save individual attachments to disk (single-click or double-click)
  - Bulk save all attachments to a selected folder
  - Handles various attachment types (documents, images, embedded emails, etc.)
- Advanced search capabilities
  - Scope: Subject (default) or Subject + Body
  - Real-time filtering with status updates
- Sort by clicking column headers (Subject/From/Date)
  - Click again to toggle ascending/descending
  - Sort glyph (▲/▼) indicates direction
- Status bar displays result count and current filter
- Context menu on items: Copy Subject, Copy From, Delete

## Download & Installation

### Option 1: Download Pre-built Installer (Recommended)

1. Go to the [Releases page](https://github.com/AlessandroPintoLiuzzi/OfflineEmailManager/releases)
2. Download the latest `OfflineEmailManager-vX.X.X-win-x64.zip`
3. Extract and run `OfflineEmailManager.exe`

*No .NET installation required - self-contained executable*

### Option 2: Build from Source

**Prerequisites:**
- Windows 10/11
- .NET 8 SDK
- WebView2 Runtime (Evergreen). If not already installed, get it from Microsoft.

**Build and run**
- From the project folder:
# Build
 dotnet build

# Run
 dotnet run
## Tech stack

- .NET 8 (WPF)
- Entity Framework Core (SQLite) — `Microsoft.EntityFrameworkCore.Sqlite`
- MIME parsing — `MimeKit`
- HTML viewer — `Microsoft.Web.WebView2`

## Using the app

1. **Load emails**
   - Click "Load .eml Files" and select one or more `.eml` files
   - If a duplicate (same subject) is found, choose Skip or Overwrite; optionally apply to all
   - Attachments are automatically extracted and stored during import

2. **Browse emails**
   - The left list shows Subject, From, Date; select an item to view
   - Email content renders in the right pane

3. **View and manage attachments**
   - When an email with attachments is selected, the attachment panel appears
   - View attachment details: filename, content type, and size
   - **Save single attachment**: Select and click "Save Selected" or double-click
   - **Save all attachments**: Click "Save All" to choose a folder and save everything
   - Supports all attachment types including embedded email messages (.eml)

4. **Search**
   - Enter text and click Search
   - Choose scope via radio buttons: Subject (default) or Subject + Body
   - Click Clear to reset the filter

5. **Sort**
   - Click Subject/From/Date column headers to sort; click again to toggle
   - ▲ / ▼ indicates the current sort direction

6. **Context menu**
   - Right-click an item to Copy Subject, Copy From, or Delete the email

## Database

- SQLite file name: `local_emails.db`
- Default location: depends on the working directory when running
  - When running from Visual Studio / `dotnet run`, the DB typically resides under `bin/<Config>/net8.0-windows/`
- You can open it with tools like DB Browser for SQLite or SQLiteStudio
- Code uses `EnsureCreated()` (no migrations required)
- **Attachment storage**: Binary data is stored directly in the database for portability

If you want to lock the DB location to a fixed path, update the connection string in `Data/EmailDbContext.cs` (e.g., provide an absolute path).

## Troubleshooting

- **Build file lock (MSB3026)**: close the running app before rebuilding, then try again
- **WebView2 not rendering**: install the WebView2 Evergreen runtime
- **Database seems empty after restart**:
  - Verify which `local_emails.db` is being used (project root vs `bin/...`)
  - Consider using an absolute path in `EmailDbContext`
- **Slow large imports**:
  - You can temporarily disable `AutoDetectChanges` during bulk adds for faster imports (already hinted in code)
- **Attachment save failures**: Check folder permissions and available disk space
- **Large attachment performance**: Database stores binary data; very large attachments may impact performance

## Project structure

- `MainWindow.xaml` — UI layout (list, search, status bar, WebView2 viewer, attachment panel)
- `MainWindow.xaml.cs` — app logic (import, search, sort, duplicate handling, attachment management)
- `Data/EmailDbContext.cs` — EF Core DbContext for SQLite
- `Model/Email.cs` — Email entity (Subject, From, Date, BodyHtml, BodyText, Attachments)
- `Model/Attachment.cs` — Attachment entity (FileName, ContentType, Size, Data)

## Automated Releases

This project uses GitHub Actions for automated builds and releases:
- Push a tag (e.g., `v1.0.0`) to trigger automatic build
- Self-contained executables are created and published to GitHub Releases
- No manual build process required for distribution

## License

MIT License

Copyright (c) 2025 Alessandro Pinto Liuzzi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.


