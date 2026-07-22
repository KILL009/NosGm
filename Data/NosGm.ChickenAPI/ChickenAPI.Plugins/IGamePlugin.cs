namespace ChickenAPI.Plugins
{
    public interface IGamePlugin : IPlugin
    {
        /// <summary>
        ///     Called when this plugin is loaded but before it has been enabled
        /// </summary>
        void OnLoad();
    }
}