using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;


namespace Smtp4qaService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller1 : Installer
    {
        public ProjectInstaller1()
        {
            InitializeComponent();
        }
    }
}
