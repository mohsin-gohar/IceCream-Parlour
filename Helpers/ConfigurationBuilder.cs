namespace Ice_Cream_Parlour_Eproject.Helpers
{
    /// <summary>
    /// Configuration helper for building IConfiguration.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddCustomConfiguration(this IConfigurationBuilder builder, IWebHostEnvironment environment)
        {
            builder.SetBasePath(environment.ContentRootPath)
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables();

            // Override with command line arguments if provided
            builder.AddCommandLine(Environment.GetCommandLineArgs());

            return builder;
        }
    }
}
