namespace Farmer.Client.Controls
{
    /// <summary>Implemented by every tab so the main window can reload its data when it is shown.</summary>
    internal interface IRefreshable
    {
        void RefreshData();
    }
}
