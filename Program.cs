internal class Program
{
    private static void Main(string[] args)
    {
        // create the container and add dependencies (transient:1 or singleton:0) 
        var container = new DependencyContainer();
        container.AddTransient<ServiceConsumer>();
        container.AddTransient<HelloService>();
        container.AddSingleTon<MessageService>();

        // create the resolver and pass the previous container
        var resolver = new DependencyResolver(container);

        // create different services to test the random int 
        var service1 = resolver.GetService<ServiceConsumer>();
        var service2 = resolver.GetService<ServiceConsumer>();
        var service3 = resolver.GetService<ServiceConsumer>();

        // call the method to print the messages
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

    // Singleton or transient
    public DependencyLifeTime LifeTime { get; set; }

    // an object of this Type
    public object Implementation { get; set; }

    // a flag to test if the dependency have an implementation already or not
    public bool Implemented { get; set; }

    public void Implement(object i)
    {
        Implementation = i;
        Implemented = true;
    }
}

public enum DependencyLifeTime
{
    // one implementation for all instances
    Singleton = 0,

    // new implementation for every instanse
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
        // get the dependency
        var dependency = _container.GetDependency(type);

        // get the constructor and throw if theres more than single constructor
        var constructor = dependency.Type.GetConstructors().Single();

        // get an array of parameters of the previous constructor
        var parameters = constructor.GetParameters().ToArray();

        if (parameters.Length > 0)
        {
            // the implementation of every parameter
            var parametersImp = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                // recursion call if the constructor have parameters
                parametersImp[i] = GetService(parameters[i].ParameterType);
            }

            // if the constructor have parameters
            return CreateImplementation(dependency, t => Activator.CreateInstance(t, parametersImp)!);
        }

        // if the constructor is parameterless 
        return CreateImplementation(dependency, t => Activator.CreateInstance(dependency.Type)!);
    }

    // this method will deal with singleton instances
    public object CreateImplementation(Dependency dep, Func<Type, object> factory)
    {
        // if it is of lifetime type of singleton it will implemented already so we use that implementation 
        if (dep.Implemented)
        {
            return dep.Implementation;
        }

        // we create an implemention
        var imp = factory(dep.Type);

        // if it has a lifetime type of singleton we add the previous implementation (imp) to the dependency.Implementation
        // so the rest of the calls will return that implementation from the previous if statement (line: 95)
        if (dep.LifeTime == DependencyLifeTime.Singleton)
        {
            dep.Implement(imp);
        }

        // we return the implemention (imp) if this dependecy is not a transient dependency
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

    // we add a dependency with a lifetime type of singleton
    public void AddSingleTon<T>()
    {
        _dependencies.Add(new Dependency(typeof(T), DependencyLifeTime.Singleton));
    }

    // we add a dependency with a lifetime type of transient
    public void AddTransient<T>()
    {
        _dependencies.Add(new Dependency(typeof(T), DependencyLifeTime.Transient));
    }

    // get the first dependency with the type specified
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

