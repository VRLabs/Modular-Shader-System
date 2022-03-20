namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Constants used across the library.
    /// </summary>
    /// <remarks>
    /// These constants are the default one used, when you embed the library you have the option to change these defaults to whatever you want
    /// </remarks>
    public static class MSSConstants
    {
        /// <summary>
        /// Default Keyword used for placing variables, all modules that do not define custom keywords to place variables will use this one.
        /// </summary>
        public const string DEFAULT_VARIABLES_KEYWORD = "DEFAULT_VARIABLES";
        
        /// <summary>
        /// Default Keyword used for placing code templates, all modules that do not define custom keywords to place code templates will use this one.
        /// </summary>
        public const string DEFAULT_CODE_KEYWORD = "DEFAULT_CODE";
        
        /// <summary>
        /// Default Keyword used for placing properties from templates.
        /// </summary>
        public const string TEMPLATE_PROPERTIES_KEYWORD = "SHADER_PROPERTIES";
        
        /// <summary>
        /// Default extension for templates.
        /// </summary>
        public const string TEMPLATE_EXTENSION = "stemplate";
        
        /// <summary>
        /// Default extension for template collections.
        /// </summary>
        public const string TEMPLATE_COLLECTION_EXTENSION = "stemplatecollection";
        
        /// <summary>
        /// Default path in the menu to place all windows menu options.
        /// </summary>
        public const string WINDOW_PATH = "VRLabs/Modular Shader";
        
        /// <summary>
        /// Default path in the create menu to place all options related to asset creation (new templates, template collections, modules, modular shaders).
        /// </summary>
        public const string CREATE_PATH = "Shader/VRLabs/Modular Shader";
        
        /// <summary>
        /// Default name of the subfolder of the Resources folder containing all the resources needed for the library.
        /// </summary>
        /// <remarks>
        /// A custom folder name is needed to differentiate different installed versions of the libraries from multiple shaders, you HAVE to use a different name when embedding the library to your shader project,
        /// or else conflicts may arise whenever someone who already has the official library will also download your shader, especially when the 2 versions don't match up.
        /// </remarks>
        public const string RESOURCES_FOLDER = "MSS";
    }
}