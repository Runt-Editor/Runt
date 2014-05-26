using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Runt.ViewModels.Kvm
{
    public class AliasManagerViewModel : Screen
    {
        readonly BindableCollection<Runt.Kvm.KAlias> _aliases;

        public AliasManagerViewModel()
        {
            DisplayName = "KVM Aliases";
            _aliases = new BindableCollection<Runt.Kvm.KAlias>(Runt.Kvm.ListAlias());
        }

        public BindableCollection<Runt.Kvm.KAlias> Aliases
        {
            get { return _aliases; }
        }
    }
}
