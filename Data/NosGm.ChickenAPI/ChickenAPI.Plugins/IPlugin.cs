namespace ChickenAPI.Plugins
{
    public interface IPlugin
    {
        /// <summary>
        ///     Name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Called when this plugin is disabled
        /// </summary>
        void OnDisable();

        /// <summary>
        ///     Called when this plugin is enabled
        /// </summary>
        void OnEnable();
    }
}