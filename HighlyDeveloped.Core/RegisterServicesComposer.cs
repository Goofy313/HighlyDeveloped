using HighlyDeveloped.Core.Interfaces;
using HighlyDeveloped.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace HighlyDeveloped.Core
{
    //inherit IUserComposer from using Umbraco.Core.Composing
    public class RegisterServicesComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Register<IEmailService, EmailService>(Lifetime.Request);
        }
    }
}
