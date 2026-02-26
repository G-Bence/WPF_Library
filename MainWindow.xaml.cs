using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WPF_Library
{
    public partial class MainWindow : Window
    {

        private readonly string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mini_konyvtar.sqlite");
        private string ConnectionString => $"Data Source={_dbPath}";

        private const int LIBRARY_READER_ID = 9999;
        private const string LIBRARY_READER_NAME = "könyvtár";

        private StackPanel _selectorListPanel;
        private bool _uiInternalChange = false;

        private int? _selectedBookIdForModify = null;
        private int? _selectedBookIdForDelete = null;
        private int? _selectedReaderIdForModify = null;
        private int? _selectedReaderIdForDelete = null;

        public MainWindow()
        {
            InitializeComponent();

            if (!File.Exists(_dbPath))
            {
                MessageBox.Show(
                    $"Database file not found:\n{_dbPath}\n\n" +
                    "Make sure mini_konyvtar.sqlite is added to the project and Copy to Output Directory is set to 'Copy if newer'.",
                    "Database missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            BuildScrollableVerticalSelector();
            ConfigureControls();
            WireEvents();
            HideAllSections();
        }

        private class BookItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Author { get; set; } = "";
            public int ReaderId { get; set; }
            public string ReaderName { get; set; } = "";
            public DateTime? BorrowDate { get; set; }

            public override string ToString()
            {

                return Title;
            }
        }

        private class ReaderItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";

            public override string ToString()
            {
                return Name;
            }
        }

        private void BuildScrollableVerticalSelector()
        {

            _selectorListPanel = new StackPanel();

            var sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _selectorListPanel
            };

            VerticalSelector.Children.Clear();
            VerticalSelector.Children.Add(sv);
        }

        private void ConfigureControls()
        {

            BookTitle.IsReadOnly = true;
            BookAuthor.IsReadOnly = true;
            BookLibUser.IsReadOnly = true;
            BorrowDP.IsEnabled = false;

            OverdueLibUser.IsReadOnly = true;
        }

        private void WireEvents()
        {

            ShowBooksBtn.Click += (s, e) => OpenListBooksScreen();
            NewBooksBtn.Click += (s, e) => OpenAddBookScreen();
            ModBooksBtn.Click += (s, e) => OpenModifyBookScreen();
            DelBooksBtn.Click += (s, e) => OpenDeleteBookScreen();

            NewReaderBtn.Click += (s, e) => OpenAddReaderScreen();
            ModReaderBtn.Click += (s, e) => OpenModifyReaderScreen();
            DelReaderBtn.Click += (s, e) => OpenDeleteReaderScreen();

            NewBorrowBtn.Click += (s, e) => OpenBorrowScreen();
            ResetBorrowBtn.Click += (s, e) => OpenResetBorrowScreen();
            ListOverduesBtn.Click += (s, e) => OpenOverduesScreen();

            ExitBtn.Click += (s, e) => Close();

            AddNewBtn.Click += AddNewBook_Click;
            ModSaveBtn.Click += SaveModifiedBook_Click;
            DeleteBookBtn.Click += DeleteBook_Click;

            AddNewReaderBtn.Click += AddNewReader_Click;
            ModSaveeaderBtn.Click += SaveModifiedReader_Click;
            DeleteReaderBtn.Click += DeleteReader_Click;

            SaveBorrowBTM.Click += SaveBorrow_Click;
            SaveResetBTM.Click += SaveResetBorrow_Click;

            BookSelectLB.SelectionChanged += BookSelectLB_SelectionChanged;
            BookSelectedLB.SelectionChanged += BookSelectedLB_SelectionChanged;
            ReaderSelectLB.SelectionChanged += ReaderSelectLB_SelectionChanged;
            ReaderSelectedLB.SelectionChanged += ReaderSelectedLB_SelectionChanged;

            ReaderSelectResetLB.SelectionChanged += ReaderSelectResetLB_SelectionChanged;
            ReaderSelectedResetLB.SelectionChanged += ReaderSelectedResetLB_SelectionChanged;
            BookSelectResetLB.SelectionChanged += BookSelectResetLB_SelectionChanged;
            BookSelectedResetLB.SelectionChanged += BookSelectedResetLB_SelectionChanged;
        }

        private void HideAllSections()
        {
            VerticalSelector.Visibility = Visibility.Collapsed;

            ShowBookSP.Visibility = Visibility.Collapsed;
            NewBookSP.Visibility = Visibility.Collapsed;
            BookModSP.Visibility = Visibility.Collapsed;
            BookDeletSP.Visibility = Visibility.Collapsed;
            NewReaderSP.Visibility = Visibility.Collapsed;
            ReaderModSP.Visibility = Visibility.Collapsed;
            ReaderDeletSP.Visibility = Visibility.Collapsed;
            BookBorrowSP.Visibility = Visibility.Collapsed;
            ResetSP.Visibility = Visibility.Collapsed;
            OverduesSP.Visibility = Visibility.Collapsed;
        }

        private void ShowSection(StackPanel panel, bool showVerticalSelector)
        {
            HideAllSections();
            VerticalSelector.Visibility = showVerticalSelector ? Visibility.Visible : Visibility.Collapsed;
            panel.Visibility = Visibility.Visible;
        }

        private void ClearVerticalSelector()
        {
            _selectorListPanel.Children.Clear();
        }

        private void AddSelectorButton(string text, string tooltip, Action onClick)
        {
            var btn = new Button
            {
                Content = text,
                ToolTip = tooltip,
                Margin = new Thickness(6, 4, 6, 0),
                Height = 28
            };

            btn.Click += (s, e) => onClick();
            _selectorListPanel.Children.Add(btn);
        }

        private void ShowInfo(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private object ToDbDate(DateTime? dt)
        {
            return dt.HasValue ? dt.Value.ToString("yyyy-MM-dd") : (object)DBNull.Value;
        }

        private DateTime? ReadNullableDate(SqliteDataReader r, string columnName)
        {
            int ord = r.GetOrdinal(columnName);
            if (r.IsDBNull(ord))
                return null;

            object val = r.GetValue(ord);

            if (val is DateTime d)
                return d;

            string s = Convert.ToString(val);
            if (DateTime.TryParse(s, out DateTime parsed))
                return parsed;

            return null;
        }

        private int ReadInt(SqliteDataReader r, string columnName)
        {
            return Convert.ToInt32(r[columnName]);
        }

        private string ReadString(SqliteDataReader r, string columnName)
        {
            return Convert.ToString(r[columnName]) ?? "";
        }

        private SqliteConnection CreateConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        private List<BookItem> GetAllBooks()
        {
            var result = new List<BookItem>();

            const string sql = @"
                SELECT
                    k.konyv_id,
                    k.cim,
                    k.szerzo,
                    k.kinel_van,
                    k.mikortol,
                    IFNULL(o.nev, '[ID: ' || k.kinel_van || ']') AS olvaso_nev
                FROM konyv k
                LEFT JOIN olvaso o ON o.olvaso_id = k.kinel_van
                ORDER BY k.cim, k.szerzo;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new BookItem
                        {
                            Id = ReadInt(r, "konyv_id"),
                            Title = ReadString(r, "cim"),
                            Author = ReadString(r, "szerzo"),
                            ReaderId = ReadInt(r, "kinel_van"),
                            ReaderName = ReadString(r, "olvaso_nev"),
                            BorrowDate = ReadNullableDate(r, "mikortol")
                        });
                    }
                }
            }

            return result;
        }

        private BookItem GetBookById(int id)
        {
            const string sql = @"
                SELECT
                    k.konyv_id,
                    k.cim,
                    k.szerzo,
                    k.kinel_van,
                    k.mikortol,
                    IFNULL(o.nev, '[ID: ' || k.kinel_van || ']') AS olvaso_nev
                FROM konyv k
                LEFT JOIN olvaso o ON o.olvaso_id = k.kinel_van
                WHERE k.konyv_id = @id;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new BookItem
                        {
                            Id = ReadInt(r, "konyv_id"),
                            Title = ReadString(r, "cim"),
                            Author = ReadString(r, "szerzo"),
                            ReaderId = ReadInt(r, "kinel_van"),
                            ReaderName = ReadString(r, "olvaso_nev"),
                            BorrowDate = ReadNullableDate(r, "mikortol")
                        };
                    }
                }
            }

            return null;
        }

        private List<BookItem> GetAvailableBooksOnly()
        {
            return GetAllBooks()
                .Where(b => b.ReaderId == LIBRARY_READER_ID)
                .ToList();
        }

        private List<BookItem> GetBorrowedBooksByReaderId(int readerId)
        {
            var result = new List<BookItem>();

            const string sql = @"
                SELECT
                    k.konyv_id, k.cim, k.szerzo, k.kinel_van, k.mikortol,
                    IFNULL(o.nev, '[ID: ' || k.kinel_van || ']') AS olvaso_nev
                FROM konyv k
                LEFT JOIN olvaso o ON o.olvaso_id = k.kinel_van
                WHERE k.kinel_van = @readerId
                ORDER BY k.cim, k.szerzo;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@readerId", readerId);
                conn.Open();

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new BookItem
                        {
                            Id = ReadInt(r, "konyv_id"),
                            Title = ReadString(r, "cim"),
                            Author = ReadString(r, "szerzo"),
                            ReaderId = ReadInt(r, "kinel_van"),
                            ReaderName = ReadString(r, "olvaso_nev"),
                            BorrowDate = ReadNullableDate(r, "mikortol")
                        });
                    }
                }
            }

            return result;
        }

        private List<ReaderItem> GetReaders(bool includeLibraryReader = false)
        {
            var result = new List<ReaderItem>();

            string sql = includeLibraryReader
                ? "SELECT olvaso_id, nev FROM olvaso ORDER BY nev;"
                : "SELECT olvaso_id, nev FROM olvaso WHERE olvaso_id <> @libId ORDER BY nev;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                if (!includeLibraryReader)
                    cmd.Parameters.AddWithValue("@libId", LIBRARY_READER_ID);

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new ReaderItem
                        {
                            Id = ReadInt(r, "olvaso_id"),
                            Name = ReadString(r, "nev")
                        });
                    }
                }
            }

            return result;
        }

        private ReaderItem GetReaderById(int id)
        {
            const string sql = "SELECT olvaso_id, nev FROM olvaso WHERE olvaso_id = @id;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new ReaderItem
                        {
                            Id = ReadInt(r, "olvaso_id"),
                            Name = ReadString(r, "nev")
                        };
                    }
                }
            }

            return null;
        }

        private bool TryResolveReaderIdFromInput(string input, out int readerId)
        {
            readerId = -1;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();

            if (int.TryParse(input, out int numericId))
            {
                var exists = GetReaderById(numericId);
                if (exists != null)
                {
                    readerId = numericId;
                    return true;
                }
                return false;
            }

            if (input.Equals(LIBRARY_READER_NAME, StringComparison.OrdinalIgnoreCase) ||
                input.Equals("konyvtar", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("library", StringComparison.OrdinalIgnoreCase))
            {
                readerId = LIBRARY_READER_ID;
                return true;
            }

            var readers = GetReaders(includeLibraryReader: true);
            var match = readers.FirstOrDefault(r => r.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                readerId = match.Id;
                return true;
            }

            return false;
        }

        private void FillShowBookForm(BookItem b)
        {
            if (b == null) return;

            BookTitle.Text = b.Title;
            BookAuthor.Text = b.Author;
            BookLibUser.Text = $"{b.ReaderName} (ID: {b.ReaderId})";
            BorrowDP.SelectedDate = b.BorrowDate;
        }

        private void FillModifyBookForm(BookItem b)
        {
            if (b == null) return;

            _selectedBookIdForModify = b.Id;

            ModBookTitle.Text = b.Title;
            ModBookAuthor.Text = b.Author;

            ModBookLibUser.Text = b.ReaderId.ToString();

            ModBorrowDP.SelectedDate = b.BorrowDate;
        }

        private void FillDeleteBookForm(BookItem b)
        {
            if (b == null) return;

            _selectedBookIdForDelete = b.Id;
            BookNameAndAuthor.Text = $"{b.Title} - {b.Author}";
        }

        private void FillModifyReaderForm(ReaderItem r)
        {
            if (r == null) return;

            _selectedReaderIdForModify = r.Id;
            ModReaderName.Text = r.Name;
        }

        private void FillDeleteReaderForm(ReaderItem r)
        {
            if (r == null) return;

            _selectedReaderIdForDelete = r.Id;
            ReaderName.Text = r.Name;
        }

        private void ClearNewBookForm()
        {
            NewBookTitle.Clear();
            NewBookAuthor.Clear();
            NewBookLibUser.Clear();
            NewBorrowDP.SelectedDate = null;
        }

        private void ClearNewReaderForm()
        {
            NewReaderName.Clear();
        }

        private void ClearModifyBookSelection()
        {
            _selectedBookIdForModify = null;
            ModBookTitle.Clear();
            ModBookAuthor.Clear();
            ModBookLibUser.Clear();
            ModBorrowDP.SelectedDate = null;
        }

        private void ClearDeleteBookSelection()
        {
            _selectedBookIdForDelete = null;
            BookNameAndAuthor.Text = "";
        }

        private void ClearModifyReaderSelection()
        {
            _selectedReaderIdForModify = null;
            ModReaderName.Clear();
        }

        private void ClearDeleteReaderSelection()
        {
            _selectedReaderIdForDelete = null;
            ReaderName.Text = "";
        }

        private void OpenListBooksScreen()
        {
            ShowSection(ShowBookSP, showVerticalSelector: true);

            ClearVerticalSelector();
            var books = GetAllBooks();

            foreach (var b in books)
            {
                string buttonText = b.Title;
                string tooltip = $"{b.Title} - {b.Author}";
                AddSelectorButton(buttonText, tooltip, () =>
                {
                    var fresh = GetBookById(b.Id);
                    FillShowBookForm(fresh);
                });
            }

            BookTitle.Clear();
            BookAuthor.Clear();
            BookLibUser.Clear();
            BorrowDP.SelectedDate = null;
        }

        private void OpenAddBookScreen()
        {
            ShowSection(NewBookSP, showVerticalSelector: false);
            ClearNewBookForm();
        }

        private void AddNewBook_Click(object sender, RoutedEventArgs e)
        {
            string title = NewBookTitle.Text.Trim();
            string author = NewBookAuthor.Text.Trim();
            string readerInput = NewBookLibUser.Text.Trim();
            DateTime? borrowDate = NewBorrowDP.SelectedDate;

            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(author) ||
                string.IsNullOrWhiteSpace(readerInput))
            {
                ShowError("Please fill every required field.");
                return;
            }

            if (!TryResolveReaderIdFromInput(readerInput, out int readerId))
            {
                ShowError("Reader is not valid. Enter an existing reader name or reader ID (or 9999 / könyvtár).");
                return;
            }

            if (readerId != LIBRARY_READER_ID && borrowDate == null)
            {
                ShowError("Please select the borrow date when the book is borrowed.");
                return;
            }

            if (readerId == LIBRARY_READER_ID)
                borrowDate = null;

            const string sql = @"
                INSERT INTO konyv (szerzo, cim, kinel_van, mikortol)
                VALUES (@author, @title, @readerId, @borrowDate);";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@author", author);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@readerId", readerId);
                cmd.Parameters.AddWithValue("@borrowDate", ToDbDate(borrowDate));

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            ShowInfo("New book added successfully.");
            ClearNewBookForm();
        }

        private void OpenModifyBookScreen()
        {
            ShowSection(BookModSP, showVerticalSelector: true);
            ClearModifyBookSelection();
            ClearVerticalSelector();

            var books = GetAllBooks();

            foreach (var b in books)
            {
                AddSelectorButton(b.Title, $"{b.Title} - {b.Author}", () =>
                {
                    var fresh = GetBookById(b.Id);
                    FillModifyBookForm(fresh);
                });
            }
        }

        private void SaveModifiedBook_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBookIdForModify == null)
            {
                ShowError("Please select a book first from the list.");
                return;
            }

            string title = ModBookTitle.Text.Trim();
            string author = ModBookAuthor.Text.Trim();
            string readerInput = ModBookLibUser.Text.Trim();
            DateTime? borrowDate = ModBorrowDP.SelectedDate;

            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(author) ||
                string.IsNullOrWhiteSpace(readerInput))
            {
                ShowError("Please fill every required field.");
                return;
            }

            if (!TryResolveReaderIdFromInput(readerInput, out int readerId))
            {
                ShowError("Reader is not valid. Enter an existing reader name or reader ID (or 9999 / könyvtár).");
                return;
            }

            if (readerId != LIBRARY_READER_ID && borrowDate == null)
            {
                ShowError("Please select the borrow date when the book is borrowed.");
                return;
            }

            if (readerId == LIBRARY_READER_ID)
                borrowDate = null;

            const string sql = @"
                UPDATE konyv
                SET cim = @title,
                    szerzo = @author,
                    kinel_van = @readerId,
                    mikortol = @borrowDate
                WHERE konyv_id = @id;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@author", author);
                cmd.Parameters.AddWithValue("@readerId", readerId);
                cmd.Parameters.AddWithValue("@borrowDate", ToDbDate(borrowDate));
                cmd.Parameters.AddWithValue("@id", _selectedBookIdForModify.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            ShowInfo("Book modified successfully.");

            OpenModifyBookScreen();
        }

        private void OpenDeleteBookScreen()
        {
            ShowSection(BookDeletSP, showVerticalSelector: true);
            ClearDeleteBookSelection();
            ClearVerticalSelector();

            var books = GetAllBooks();

            foreach (var b in books)
            {
                AddSelectorButton(b.Title, $"{b.Title} - {b.Author}", () =>
                {
                    var fresh = GetBookById(b.Id);
                    FillDeleteBookForm(fresh);
                });
            }
        }

        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBookIdForDelete == null)
            {
                ShowError("Please select a book first from the list.");
                return;
            }

            var confirm = MessageBox.Show(
                "Are you sure you want to delete this book?",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            const string sql = "DELETE FROM konyv WHERE konyv_id = @id;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", _selectedBookIdForDelete.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            ShowInfo("Book deleted successfully.");

            OpenDeleteBookScreen();
        }

        private void OpenAddReaderScreen()
        {
            ShowSection(NewReaderSP, showVerticalSelector: false);
            ClearNewReaderForm();
        }

        private void AddNewReader_Click(object sender, RoutedEventArgs e)
        {
            string name = NewReaderName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("Please fill the reader name.");
                return;
            }

            const string sql = "INSERT INTO olvaso (nev) VALUES (@name);";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@name", name);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            ShowInfo("New reader added successfully.");
            ClearNewReaderForm();
        }

        private void OpenModifyReaderScreen()
        {
            ShowSection(ReaderModSP, showVerticalSelector: true);
            ClearModifyReaderSelection();
            ClearVerticalSelector();

            var readers = GetReaders(includeLibraryReader: false);

            foreach (var r in readers)
            {
                AddSelectorButton(r.Name, $"Reader ID: {r.Id}", () =>
                {
                    var fresh = GetReaderById(r.Id);
                    FillModifyReaderForm(fresh);
                });
            }
        }

        private void SaveModifiedReader_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReaderIdForModify == null)
            {
                ShowError("Please select a reader first from the list.");
                return;
            }

            string newName = ModReaderName.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                ShowError("Please fill the reader name.");
                return;
            }

            const string sql = "UPDATE olvaso SET nev = @name WHERE olvaso_id = @id;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@name", newName);
                cmd.Parameters.AddWithValue("@id", _selectedReaderIdForModify.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            ShowInfo("Reader modified successfully.");
            OpenModifyReaderScreen();
        }

        private void OpenDeleteReaderScreen()
        {
            ShowSection(ReaderDeletSP, showVerticalSelector: true);
            ClearDeleteReaderSelection();
            ClearVerticalSelector();

            var readers = GetReaders(includeLibraryReader: false);

            foreach (var r in readers)
            {
                AddSelectorButton(r.Name, $"Reader ID: {r.Id}", () =>
                {
                    var fresh = GetReaderById(r.Id);
                    FillDeleteReaderForm(fresh);
                });
            }
        }

        private void DeleteReader_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReaderIdForDelete == null)
            {
                ShowError("Please select a reader first from the list.");
                return;
            }

            if (_selectedReaderIdForDelete.Value == LIBRARY_READER_ID)
            {
                ShowError("The library reader (9999) cannot be deleted.");
                return;
            }

            const string countSql = "SELECT COUNT(*) FROM konyv WHERE kinel_van = @readerId;";
            int borrowedCount;

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(countSql, conn))
            {
                cmd.Parameters.AddWithValue("@readerId", _selectedReaderIdForDelete.Value);
                conn.Open();
                borrowedCount = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (borrowedCount > 0)
            {
                ShowError("This reader cannot be deleted because they still have borrowed book(s). Reset the borrow(s) first.");
                return;
            }

            var confirm = MessageBox.Show(
                "Are you sure you want to delete this reader?",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            const string deleteSql = "DELETE FROM olvaso WHERE olvaso_id = @id;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(deleteSql, conn))
            {
                cmd.Parameters.AddWithValue("@id", _selectedReaderIdForDelete.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            ShowInfo("Reader deleted successfully.");
            OpenDeleteReaderScreen();
        }

        private void OpenBorrowScreen()
        {
            ShowSection(BookBorrowSP, showVerticalSelector: false);

            _uiInternalChange = true;
            try
            {
                BookSelectLB.Items.Clear();
                BookSelectedLB.Items.Clear();
                ReaderSelectLB.Items.Clear();
                ReaderSelectedLB.Items.Clear();

                foreach (var b in GetAvailableBooksOnly())
                    BookSelectLB.Items.Add(b);

                foreach (var r in GetReaders(includeLibraryReader: false))
                    ReaderSelectLB.Items.Add(r);
            }
            finally
            {
                _uiInternalChange = false;
            }
        }

        private void BookSelectLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_uiInternalChange) return;
            if (BookSelectLB.SelectedItem is BookItem selected)
            {
                if (!BookSelectedLB.Items.OfType<BookItem>().Any(b => b.Id == selected.Id))
                    BookSelectedLB.Items.Add(selected);

                BookSelectLB.SelectedItem = null;
            }
        }

        private void BookSelectedLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_uiInternalChange) return;
            if (BookSelectedLB.SelectedItem is BookItem selected)
            {

                BookSelectedLB.Items.Remove(selected);
                BookSelectedLB.SelectedItem = null;
            }
        }

        private void ReaderSelectLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_uiInternalChange) return;
            if (ReaderSelectLB.SelectedItem is ReaderItem selected)
            {

                ReaderSelectedLB.Items.Clear();
                ReaderSelectedLB.Items.Add(selected);

                ReaderSelectLB.SelectedItem = null;
            }
        }

        private void ReaderSelectedLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_uiInternalChange) return;
            if (ReaderSelectedLB.SelectedItem is ReaderItem selected)
            {
                ReaderSelectedLB.Items.Remove(selected);
                ReaderSelectedLB.SelectedItem = null;
            }
        }

        private void SaveBorrow_Click(object sender, RoutedEventArgs e)
        {
            var selectedBooks = BookSelectedLB.Items.OfType<BookItem>().ToList();
            var selectedReaders = ReaderSelectedLB.Items.OfType<ReaderItem>().ToList();

            if (selectedBooks.Count == 0)
            {
                ShowError("Please select at least one book.");
                return;
            }

            if (selectedReaders.Count != 1)
            {
                ShowError("Please select exactly one reader. (Your database stores one reader per book.)");
                return;
            }

            var reader = selectedReaders[0];
            DateTime today = DateTime.Today;

            using (var conn = CreateConnection())
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    const string sql = @"
                        UPDATE konyv
                        SET kinel_van = @readerId,
                            mikortol = @today
                        WHERE konyv_id = @bookId;";

                    using (var cmd = new SqliteCommand(sql, conn, tx))
                    {
                        foreach (var b in selectedBooks)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@readerId", reader.Id);
                            cmd.Parameters.AddWithValue("@today", today.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@bookId", b.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }

            ShowInfo("Borrow saved successfully.\nOverdue date is automatically considered as borrow date + 1 month.");
            OpenBorrowScreen();
        }

        private void OpenResetBorrowScreen()
        {
            ShowSection(ResetSP, showVerticalSelector: false);

            _uiInternalChange = true;
            try
            {
                ReaderSelectResetLB.Items.Clear();
                ReaderSelectedResetLB.Items.Clear();
                BookSelectResetLB.Items.Clear();
                BookSelectedResetLB.Items.Clear();

                foreach (var r in GetReaders(includeLibraryReader: false))
                    ReaderSelectResetLB.Items.Add(r);
            }
            finally
            {
                _uiInternalChange = false;
            }
        }

        private void ReaderSelectResetLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_uiInternalChange) return;
            if (ReaderSelectResetLB.SelectedItem is ReaderItem selected)
            {

                ReaderSelectedResetLB.Items.Clear();
                ReaderSelectedResetLB.Items.Add(selected);

                ReaderSelectResetLB.SelectedItem = null;

                RefreshResetBorrowBookSource();
            }
        }

        private void ReaderSelectedResetLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_uiInternalChange) return;
            if (ReaderSelectedResetLB.SelectedItem is ReaderItem selected)
            {
                ReaderSelectedResetLB.Items.Remove(selected);
                ReaderSelectedResetLB.SelectedItem = null;

                RefreshResetBorrowBookSource();
            }
        }

        private void RefreshResetBorrowBookSource()
        {
            _uiInternalChange = true;
            try
            {
                BookSelectResetLB.Items.Clear();
                BookSelectedResetLB.Items.Clear();

                var selectedReader = ReaderSelectedResetLB.Items.OfType<ReaderItem>().FirstOrDefault();
                if (selectedReader == null)
                    return;

                foreach (var b in GetBorrowedBooksByReaderId(selectedReader.Id))
                    BookSelectResetLB.Items.Add(b);
            }
            finally
            {
                _uiInternalChange = false;
            }
        }

        private void BookSelectResetLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_uiInternalChange) return;

            if (BookSelectResetLB.SelectedItem is BookItem selected)
            {
                if (!BookSelectedResetLB.Items.OfType<BookItem>().Any(b => b.Id == selected.Id))
                    BookSelectedResetLB.Items.Add(selected);

                BookSelectResetLB.Items.Remove(selected);
                BookSelectResetLB.SelectedItem = null;
            }
        }

        private void BookSelectedResetLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_uiInternalChange) return;

            if (BookSelectedResetLB.SelectedItem is BookItem selected)
            {
                BookSelectedResetLB.Items.Remove(selected);

                if (!BookSelectResetLB.Items.OfType<BookItem>().Any(b => b.Id == selected.Id))
                    BookSelectResetLB.Items.Add(selected);

                BookSelectedResetLB.SelectedItem = null;
            }
        }

        private void SaveResetBorrow_Click(object sender, RoutedEventArgs e)
        {
            var selectedBooks = BookSelectedResetLB.Items.OfType<BookItem>().ToList();

            if (selectedBooks.Count == 0)
            {
                ShowError("Please select at least one book to reset.");
                return;
            }

            using (var conn = CreateConnection())
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    const string sql = @"
                        UPDATE konyv
                        SET kinel_van = @libId,
                            mikortol = NULL
                        WHERE konyv_id = @bookId;";

                    using (var cmd = new SqliteCommand(sql, conn, tx))
                    {
                        foreach (var b in selectedBooks)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@libId", LIBRARY_READER_ID);
                            cmd.Parameters.AddWithValue("@bookId", b.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }

            ShowInfo("Borrow reset successfully (book returned).");
            RefreshResetBorrowBookSource();
        }

        private List<ReaderItem> GetReadersWithOverdues()
        {
            var result = new List<ReaderItem>();

            const string sql = @"
                SELECT DISTINCT o.olvaso_id, o.nev
                FROM olvaso o
                INNER JOIN konyv k ON k.kinel_van = o.olvaso_id
                WHERE o.olvaso_id <> @libId
                  AND k.mikortol IS NOT NULL
                  AND DATE(k.mikortol, '+1 month') < DATE('now', 'localtime')
                ORDER BY o.nev;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@libId", LIBRARY_READER_ID);
                conn.Open();

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new ReaderItem
                        {
                            Id = ReadInt(r, "olvaso_id"),
                            Name = ReadString(r, "nev")
                        });
                    }
                }
            }

            return result;
        }

        private List<BookItem> GetOverdueBooksOfReader(int readerId)
        {
            var result = new List<BookItem>();

            const string sql = @"
                SELECT
                    k.konyv_id, k.cim, k.szerzo, k.kinel_van, k.mikortol,
                    o.nev AS olvaso_nev
                FROM konyv k
                INNER JOIN olvaso o ON o.olvaso_id = k.kinel_van
                WHERE k.kinel_van = @readerId
                  AND k.mikortol IS NOT NULL
                  AND DATE(k.mikortol, '+1 month') < DATE('now', 'localtime')
                ORDER BY k.cim, k.szerzo;";

            using (var conn = CreateConnection())
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@readerId", readerId);
                conn.Open();

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new BookItem
                        {
                            Id = ReadInt(r, "konyv_id"),
                            Title = ReadString(r, "cim"),
                            Author = ReadString(r, "szerzo"),
                            ReaderId = ReadInt(r, "kinel_van"),
                            ReaderName = ReadString(r, "olvaso_nev"),
                            BorrowDate = ReadNullableDate(r, "mikortol")
                        });
                    }
                }
            }

            return result;
        }

        private void OpenOverduesScreen()
        {
            ShowSection(OverduesSP, showVerticalSelector: true);
            ClearVerticalSelector();

            OverdueLibUser.Clear();
            OverdueBooksLB.Items.Clear();

            var readers = GetReadersWithOverdues();

            foreach (var r in readers)
            {
                AddSelectorButton(r.Name, $"Reader ID: {r.Id}", () =>
                {
                    OverdueLibUser.Text = r.Name;
                    OverdueBooksLB.Items.Clear();

                    var overdueBooks = GetOverdueBooksOfReader(r.Id);
                    foreach (var b in overdueBooks)
                    {
                        OverdueBooksLB.Items.Add($"{b.Title} - {b.Author}");
                    }
                });
            }

            if (readers.Count == 0)
            {
                ShowInfo("There are currently no overdue readers.");
            }
        }

    }
}
