
namespace Clowa.Client.Interface
{
    public interface IDialog
    {
        void BindViewModel<TViewModel>(TViewModel viewModel);
        void Show();
        void Close();
    }
}
