using System.Collections.Generic;
using System.Collections.ObjectModel;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AnnoDesigner.ViewModels
{
    /// <summary>
    /// Provides license information for third party assets and NuGet packages used in the project.
    /// </summary>
    public partial class LicensesViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<LicenseInfo> licenses;

        private const string APACHE_2 = "Apache-2.0 License";
        private const string MS_PL = "Microsoft Public License (Ms-PL)";
        private const string MIT = "MIT License";
        private const string BSD_3 = "BSD-3-Clause License";

        public LicensesViewModel()
        {
            Licenses = new ObservableCollection<LicenseInfo>
            {
                // Project itself
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/AnnoDesigner/anno-designer/blob/master/LICENSE",
                    ProjectName = "Anno Designer (this project)",
                    ProjectWebsite = "https://github.com/AnnoDesigner/anno-designer",
                    Assets = null
                },

                // Icons (original project assets)
                new LicenseInfo
                {
                    License = APACHE_2,
                    LicenseURL = "https://github.com/google/material-design-icons/blob/master/LICENSE",
                    ProjectName = "Material Design Icons",
                    ProjectWebsite = "https://github.com/google/material-design-icons",
                    Assets = new List<string>
                    {
                        "left-click.png",
                        "middle-click.png",
                        "right-click.png",
                        "chevron-up.png"
                    }
                },

                // Extended WPF Toolkit
                new LicenseInfo
                {
                    License = MS_PL,
                    LicenseURL = "https://licenses.nuget.org/MS-PL",
                    ProjectName = "Extended WPF Toolkit™ (Xceed.Wpf.Toolkit)",
                    ProjectWebsite = "https://github.com/xceedsoftware/wpftoolkit",
                    Assets = null
                },

                // Newtonsoft.Json
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md",
                    ProjectName = "Newtonsoft.Json (Json.NET)",
                    ProjectWebsite = "https://www.newtonsoft.com/json",
                    Assets = null
                },

                // NLog
                new LicenseInfo
                {
                    License = BSD_3,
                    LicenseURL = "https://github.com/NLog/NLog/blob/master/LICENSE.md",
                    ProjectName = "NLog",
                    ProjectWebsite = "https://nlog-project.org/",
                    Assets = null
                },

                // Octokit
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/octokit/octokit.net/blob/master/LICENSE",
                    ProjectName = "Octokit.NET",
                    ProjectWebsite = "https://github.com/octokit/octokit.net",
                    Assets = null
                },

                // Microsoft.Bcl.HashCode
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/dotnet/runtime/blob/main/LICENSE.TXT",
                    ProjectName = "Microsoft.Bcl.HashCode",
                    ProjectWebsite = "https://www.nuget.org/packages/Microsoft.Bcl.HashCode",
                    Assets = null
                },

                // Microsoft.Xaml.Behaviors
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/microsoft/XamlBehaviorsWpf/blob/main/LICENSE",
                    ProjectName = "Microsoft.Xaml.Behaviors",
                    ProjectWebsite = "https://github.com/microsoft/XamlBehaviorsWpf",
                    Assets = null
                },

                // System.CommandLine
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/dotnet/command-line-api/blob/main/LICENSE.TXT",
                    ProjectName = "System.CommandLine",
                    ProjectWebsite = "https://github.com/dotnet/command-line-api",
                    Assets = null
                },

                // System.* (System.Buffers / System.Memory / System.Numerics.Vectors / Unsafe)
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/dotnet/runtime/blob/main/LICENSE.TXT",
                    ProjectName = "System.* runtime assemblies (Microsoft)",
                    ProjectWebsite = "https://github.com/dotnet/runtime",
                    Assets = null
                },

                // System.IO.Abstractions
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/TestableIO/System.IO.Abstractions/blob/master/LICENSE",
                    ProjectName = "System.IO.Abstractions",
                    ProjectWebsite = "https://github.com/TestableIO/System.IO.Abstractions",
                    Assets = null
                },

                // WikiClientLibrary (used by FandomParser)
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/CXuesong/WikiClientLibrary/blob/master/LICENSE",
                    ProjectName = "WikiClientLibrary",
                    ProjectWebsite = "https://github.com/CXuesong/WikiClientLibrary",
                    Assets = null
                },

                // Wpf.Ui (used for message boxes)
                new LicenseInfo
                {
                    License = MIT,
                    LicenseURL = "https://github.com/lepoco/wpfui/blob/main/LICENSE",
                    ProjectName = "Wpf.Ui",
                    ProjectWebsite = "https://github.com/lepoco/wpfui",
                    Assets = null
                }
            };
        }
    }
}
