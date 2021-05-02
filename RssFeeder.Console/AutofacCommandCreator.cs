using System;
using Autofac;
using Oakton;

namespace RssFeeder.Console
{
    public class AutofacCommandCreator : ICommandCreator
    {
        private readonly IContainer _container;

        public AutofacCommandCreator(IContainer container)
        {
            _container = container;
        }

        public IOaktonCommand CreateCommand(Type commandType)
        {
            return (IOaktonCommand)_container.Resolve(commandType, new TypedParameter(typeof(IContainer), _container));
        }

        public object CreateModel(Type modelType)
        {
            return _container.Resolve(modelType);
        }
    }
}