using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Settings = NINA.Plugin.Logviewer.Properties.Settings;

namespace NINA.Plugin.Logviewer;

[Export(typeof(IPluginManifest))]
public class NinaPluginLogviewer : PluginBase, INotifyPropertyChanged {
    private readonly IPluginOptionsAccessor pluginSettings;
    private readonly IProfileService profileService;
    public MainViewModel LogVM { get; }

    [ImportingConstructor]
    public NinaPluginLogviewer(IProfileService profileService, IOptionsVM options /* , IImageSaveMediator imageSaveMediator*/ ) {
        if (Settings.Default.UpdateSettings) {
            Settings.Default.Upgrade();
            Settings.Default.UpdateSettings = false;
            CoreUtil.SaveSettings(Settings.Default);
        }
        LogVM = new MainViewModel(new LogService());
        // This helper class can be used to store plugin settings that are dependent on the current profile
        this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
        this.profileService = profileService;
        // React on a changed profile
        profileService.ProfileChanged += ProfileService_ProfileChanged;
    }

    public override Task Teardown() {
        profileService.ProfileChanged -= ProfileService_ProfileChanged;
        return base.Teardown();
    }

    private void ProfileService_ProfileChanged(object sender, EventArgs e) {
        // Rase the event that this profile specific value has been changed due to the profile switch
        //RaisePropertyChanged(nameof(ProfileSpecificNotificationMessage));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}