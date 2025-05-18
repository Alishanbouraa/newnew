// OfflinePOS.Cashier/Views/DrawerView.xaml.cs
using OfflinePOS.Cashier.ViewModels;
using System.Windows.Controls;

namespace OfflinePOS.Cashier.Views
{
    /// <summary>
    /// Interaction logic for DrawerView.xaml
    /// </summary>
    public partial class DrawerView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the DrawerView class
        /// </summary>
        public DrawerView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the DrawerView class with the specified ViewModel
        /// </summary>
        /// <param name="viewModel">DrawerViewModel</param>
        public DrawerView(DrawerViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}