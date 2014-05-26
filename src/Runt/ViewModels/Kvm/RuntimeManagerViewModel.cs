using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Runt.ViewModels.Kvm
{
    public class RuntimeManagerViewModel : Screen
    {
        readonly BindableCollection<Runt.Kvm.KRuntime> _runtimes;

        public RuntimeManagerViewModel()
        {
            DisplayName = "KVM Runtimes";
            _runtimes = new BindableCollection<Runt.Kvm.KRuntime>(Runt.Kvm.List());
        }

        public BindableCollection<Runt.Kvm.KRuntime> Runtimes
        {
            get { return _runtimes; }
        }
    }
}
