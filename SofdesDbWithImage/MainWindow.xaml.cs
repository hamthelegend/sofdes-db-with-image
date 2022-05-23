using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace SofdesDbWithImage
{
    
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private List<User> _users;
        public List<User> Users
        {
            get => _users;
            set { _users = value; OnPropertyChanged(); }
        }

        StorageFile Picture;
        public MainWindow()
        {
            InitializeComponent();
            UpdatePictureAsync();
            LoadData();
        }

        private void LoadData()
        {
            Users = UsersDb.GetAll();
        }

        private void NaturalNumbersOnlyBeforeTextChange(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
        }
        private async Task<User> ParseUserAsync()
        {
            if (string.IsNullOrEmpty(idInput.Text) ||
                string.IsNullOrEmpty(nameInput.Text) ||
                birthdayInput.SelectedDate == null ||
                Picture == null)
            {
                await new ContentDialog
                {
                    Title = "Save failed",
                    Content = "None of the fields can be empty.",
                    CloseButtonText = "Okay",
                    XamlRoot = Content.XamlRoot,
                }.ShowAsync();
                return null;
            }

            var id = int.Parse(idInput.Text);
            var name = nameInput.Text;
            var birthday = birthdayInput.Date;

            using var inputStream = await Picture.OpenSequentialReadAsync();
            var readStream = inputStream.AsStreamForRead();
            var byteArray = new byte[readStream.Length];
            await readStream.ReadAsync(byteArray);
            return new User(id, name, birthday, byteArray);
        }

        private async void BrowsePictureAsync(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            Picture = await picker.PickSingleFileAsync();
            if (Picture != null)
            {
                UpdatePictureAsync();
            }
        }

        private async void UpdatePictureAsync()
        {
            StorageFile file;
            if (Picture == null)
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Square150x150Logo.scale-200.png"));
            }
            else
            {
                file = Picture;
            }
            using var fileStream = await file.OpenAsync(FileAccessMode.Read);
            BitmapImage bitmapImage = new();
            await bitmapImage.SetSourceAsync(fileStream);
            pictureImage.Source = bitmapImage;
        }

        private async void LoadFromIdAsync(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(idInput.Text))
            {
                await new ContentDialog
                {
                    Title = "Empty ID",
                    Content = "Input the ID of the user you want to load first.",
                    CloseButtonText = "Okay",
                    XamlRoot = Content.XamlRoot,
                }.ShowAsync();
                return;
            }
            var id = int.Parse(idInput.Text);
            var user = UsersDb.Get(id);

            if (user == null)
            {
                await new ContentDialog
                {
                    Title = "User not found",
                    Content = "User with that ID was not found.",
                    CloseButtonText = "Okay",
                    XamlRoot = Content.XamlRoot,
                }.ShowAsync();
                return;
            }

            LoadUser(user);
        }


        private async void LoadUser(object sender, RoutedEventArgs e)
        {
            var user = userInput.SelectedValue as User;
            if (user == null)
            {
                await new ContentDialog
                {
                    Title = "No user selected",
                    Content = "Please select a user from the drop down.",
                    CloseButtonText = "Okay",
                    XamlRoot = Content.XamlRoot,
                }.ShowAsync();
                return;
            }
            LoadUser(user);
        }

        private async void LoadUser(User user)
        {
            idInput.Text = user.Id.ToString();
            nameInput.Text = user.Name;
            birthdayInput.Date = user.Birthday;

            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.CreateFileAsync("picture", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(file, user.Picture);
            Picture = file;
            UpdatePictureAsync();
        }

        private async void SaveAsync(object sender, RoutedEventArgs e)
        {
            var user = await ParseUserAsync();
            if (user != null)
            {
                UsersDb.InsertUpdate(user);
                LoadData();
                Clear();
            }
        }
        private async void Delete(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(idInput.Text))
            {
                await new ContentDialog
                {
                    Title = "Empty ID",
                    Content = "Input the ID of the user you want to delete first.",
                    CloseButtonText = "Okay",
                    XamlRoot = Content.XamlRoot,
                }.ShowAsync();
                return;
            }

            var id = int.Parse(idInput.Text);
            var user = UsersDb.Get(id);
            if (user == null)
            {
                await new ContentDialog
                {
                    Title = "User not found",
                    Content = "User with that ID was not found.",
                    CloseButtonText = "Okay",
                    XamlRoot = Content.XamlRoot,
                }.ShowAsync();
                return;
            }

            var response = await new ContentDialog
            {
                Title = "Delete user",
                Content = "Are you sure you want to delete this user?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = Content.XamlRoot,
            }.ShowAsync();

            if (response == ContentDialogResult.Primary)
            {
                UsersDb.Delete(id);
                Clear();
            }
            LoadData();

        }

        private void Clear(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void Clear()
        {
            idInput.Text = string.Empty;
            nameInput.Text = string.Empty;
            birthdayInput.SelectedDate = null;
            Picture = null;
            UpdatePictureAsync();
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void usersDataGrid_AutoGeneratingColumn(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.ToString() == "Picture")
            {
                e.Cancel = true;
            }
        }

        private void birthdayInput_SelectedDateChanged(object sender, DatePickerSelectedValueChangedEventArgs e)
        {
            var birthday = e.NewDate;
            if (birthday != null)
            {
                var now = DateTimeOffset.Now;
                var age = now.Year - birthday?.Year;
                if (now.Month < birthday?.Month || (now.Month == birthday?.Month && now.Day < birthday?.Day))
                {
                    age--;
                }
                ageInput.Text = age.ToString();
            } 
            else
            {
                ageInput.Text = string.Empty;
            }
        }
    }
}
