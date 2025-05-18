// OfflinePOS.Cashier/Views/SalesView.xaml.cs
using OfflinePOS.Cashier.ViewModels;
using System.Windows.Controls;

namespace OfflinePOS.Cashier.Views
{
    /// <summary>
    /// Interaction logic for SalesView.xaml
    /// </summary>
    public partial class SalesView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the SalesView class
        /// </summary>
        public SalesView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the SalesView class with the specified ViewModel
        /// </summary>
        /// <param name="viewModel">SalesViewModel</param>
        public SalesView(SalesViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}