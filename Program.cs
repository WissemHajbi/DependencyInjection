
internal class Program
{
    private static void Main(string[] args)
    {
        var container = new DependencyContainer();
        container.AddTransient<ServiceConsumer>();
        container.AddTransient<HelloService>();
        container.AddSingleTon<MessageService>();

        var resolver = new DependencyResolver(container);

        var service1 = resolver.GetService<ServiceConsumer>();
        var service2 = resolver.GetService<ServiceConsumer>();
        var service3 = resolver.GetService<ServiceConsumer>();
        service1?.Print();
        service2?.Print();
        service3?.Print();
    }
}

public class Dependency
{
    public Dependency(Type type, DependencyLifeTime lifeTime)
    {
        Type = type;
        LifeTime = lifeTime;
    }

    public Type Type { get; set; }
    public DependencyLifeTime LifeTime { get; set; }
    public object Implementation { get; set; }
    public bool Implemented { get; set; }

    public void Implement(object i)
    {
        Implementation = i;
        Implemented = true;
    }
}

public enum DependencyLifeTime
{
    Singleton = 0,
    Transient = 1
}

class DependencyResolver
{
    private DependencyContainer _container;

    public DependencyResolver(DependencyContainer container)
    {
        _container = container;
    }

    public T GetService<T>() => (T)GetService(typeof(T));

    public object GetService(Type type)
    {
        var dependency = _container.GetDependency(type);
        var constructor = dependency.Type.GetConstructors().Single();
        var parameters = constructor.GetParameters().ToArray();

        if (parameters.Length > 0)
        {
            var parametersImp = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parametersImp[i] = GetService(parameters[i].ParameterType);
            }
            return CreateImplementation(dependency, t => Activator.CreateInstance(t, parametersImp)!);
        }

        return CreateImplementation(dependency, t => Activator.CreateInstance(dependency.Type)!);
    }

    public object CreateImplementation(Dependency dep, Func<Type, object> factory)
    {
        if (dep.Implemented)
        {
            return dep.Implementation;
        }
        var imp = factory(dep.Type);
        if (dep.LifeTime == DependencyLifeTime.Singleton)
        {
            dep.Implement(imp);
        }
        return imp;
    }
}

public class DependencyContainer
{
    List<Dependency> _dependencies;

    public DependencyContainer()
    {
        _dependencies = new List<Dependency>();
    }

    public void AddSingleTon<T>()
    {
        _dependencies.Add(new Dependency(typeof(T), DependencyLifeTime.Singleton));
    }

    public void AddTransient<T>()
    {
        _dependencies.Add(new Dependency(typeof(T), DependencyLifeTime.Transient));
    }

    public Dependency GetDependency(Type type)
    {
        return _dependencies.First(x => x.Type.Name == type.Name);
    }
}

public class MessageService
{
    int _random;
    public MessageService()
    {
        _random = new Random().Next();
    }
    public string Message()
    {
        return $"yo {_random}";
    }
}

public class HelloService
{
    private MessageService _message;

    public HelloService(MessageService message)
    {
        _message = message;
    }
    public void Print()
    {
        Console.WriteLine($"hello world {_message.Message()}");
    }
}
public class ServiceConsumer
{
    HelloService _hello;
    public ServiceConsumer(HelloService hello)
    {
        _hello = hello;
    }

    public void Print()
    {
        _hello.Print();
    }
}

