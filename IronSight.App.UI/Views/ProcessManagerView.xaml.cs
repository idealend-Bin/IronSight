using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using IronSight.Interop;
using IronSight.Interop.Core;
using IronSight.Interop.Events;
using IronSight.Interop.Native.Memory;
using IronSight.Interop.Native.System;
using IronSight.Interop.Services;


namespace IronSight.App.UI.Views
{
    public partial class ProcessManagerView : UserControl
    {
        public ProcessManagerView()
        {
            InitializeComponent();
        }
    }
}

