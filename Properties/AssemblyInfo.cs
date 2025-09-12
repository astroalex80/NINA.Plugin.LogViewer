using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin. Generate a fresh one for your plugin!
[assembly: Guid("4984595d-c3b6-456d-9b35-b11345ff9ad1")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new release build of a plugin
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Log Viewer")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("A plugin to read and filter N.I.N.A. log files.")]

// The following attributes are not required for the plugin per se, but are required by the official manifest meta data

// Your name
[assembly: AssemblyCompany("Alexander Wrede @astro_alex80")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("NINA.Plugin.LogViewer")]
[assembly: AssemblyCopyright("Copyright © 2025 Alexander Wrede")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
//[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.2017")]
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.1")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
// The repository where your pluggin is hosted
[assembly: AssemblyMetadata("Repository", "https://github.com/astroalex80/NINA.Plugin.LogViewer/")]

// The following attributes are optional for the official manifest meta data

//[Optional] Your plugin homepage URL - omit if not applicaple
[assembly: AssemblyMetadata("Homepage", "")]

//[Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "log, reader, viewer")]

//[Optional] A link that will show a log of all changes in between your plugin's versions
[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/astroalex80/NINA.Plugin.LogViewer/CHANGELOG.md")]

//[Optional] The url to a featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "/Log Viewer;component/Resources/LogViewer_Logo.png")]
//[Optional] A url to an example screenshot of your plugin in action
[assembly: AssemblyMetadata("ScreenshotURL", "/Log Viewer;component/Resources/screenshot.png")]
//[Optional] An additional url to an example example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"With the Log Viewer plugin you can open the currently active or past log files and comfortably filter for deeper analysis.")]
// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]