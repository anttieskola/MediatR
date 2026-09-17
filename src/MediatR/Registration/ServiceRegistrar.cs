using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MediatR.Registration;

public static class ServiceRegistrar
{
    private static int MaxGenericTypeParameters;
    private static int MaxTypesClosing;
    private static int MaxGenericTypeRegistrations;
    private static int RegistrationTimeout;

    public static void SetGenericRequestHandlerRegistrationLimitations(MediatRServiceConfiguration configuration)
    {
        MaxGenericTypeParameters = configuration.MaxGenericTypeParameters;
        MaxTypesClosing = configuration.MaxTypesClosing;
        MaxGenericTypeRegistrations = configuration.MaxGenericTypeRegistrations;
        RegistrationTimeout = configuration.RegistrationTimeout;
    }

    public static void AddMediatRClassesWithTimeout(IServiceCollection services, MediatRServiceConfiguration configuration)
    {
        using CancellationTokenSource cts = new(RegistrationTimeout);
        try
        {
            AddMediatRClasses(services, configuration, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("The generic handler registration process timed out.");
        }
    }

    public static void AddMediatRClasses(IServiceCollection services, MediatRServiceConfiguration configuration, CancellationToken cancellationToken = default)
    {

        Assembly[] assembliesToScan = configuration.AssembliesToRegister.Distinct().ToArray();

        ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IRequestHandler<>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>), services, assembliesToScan, true, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IStreamRequestHandler<,>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IRequestExceptionHandler<,,>), services, assembliesToScan, true, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IRequestExceptionAction<,>), services, assembliesToScan, true, configuration, cancellationToken);

        if (configuration.AutoRegisterRequestProcessors)
        {
            ConnectImplementationsToTypesClosing(typeof(IRequestPreProcessor<>), services, assembliesToScan, true, configuration, cancellationToken);
            ConnectImplementationsToTypesClosing(typeof(IRequestPostProcessor<,>), services, assembliesToScan, true, configuration, cancellationToken);
        }

        List<Type> multiOpenInterfaces =
        [
            typeof(INotificationHandler<>),
            typeof(IRequestExceptionHandler<,,>),
            typeof(IRequestExceptionAction<,>)
        ];

        if (configuration.AutoRegisterRequestProcessors)
        {
            multiOpenInterfaces.Add(typeof(IRequestPreProcessor<>));
            multiOpenInterfaces.Add(typeof(IRequestPostProcessor<,>));
        }

        foreach (Type multiOpenInterface in multiOpenInterfaces)
        {
            int arity = multiOpenInterface.GetGenericArguments().Length;

            List<Type> concretions = assembliesToScan
                .SelectMany(a => a.DefinedTypes)
                .Where(type => type.FindInterfacesThatClose(multiOpenInterface).Any())
                .Where(type => type.IsConcrete() && type.IsOpenGeneric())
                .Where(type => type.GetGenericArguments().Length == arity)
                .Where(configuration.TypeEvaluator)
                .ToList();

            foreach (Type type in concretions)
            {
                _ = services.AddTransient(multiOpenInterface, type);
            }
        }
    }

    private static void ConnectImplementationsToTypesClosing(Type openRequestInterface,
        IServiceCollection services,
        IEnumerable<Assembly> assembliesToScan,
        bool addIfAlreadyExists,
        MediatRServiceConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        (List<Type>? concretions, List<Type>? interfaces, List<Type>? genericConcretions, List<Type>? genericInterfaces) =
            ClassifyTypes(openRequestInterface, assembliesToScan, configuration);

        foreach (Type @interface in interfaces)
        {
            RegisterExactMatches(@interface, concretions, services, addIfAlreadyExists);
        }

        foreach (Type @interface in genericInterfaces)
        {
            List<Type> exactMatches = genericConcretions.Where(x => x.CanBeCastTo(@interface)).ToList();
            AddAllConcretionsThatClose(@interface, exactMatches, services, assembliesToScan, cancellationToken);
        }
    }

    private static (List<Type> Concretions, List<Type> Interfaces, List<Type> GenericConcretions, List<Type> GenericInterfaces)
        ClassifyTypes(Type openRequestInterface, IEnumerable<Assembly> assembliesToScan, MediatRServiceConfiguration configuration)
    {
        List<Type> concretions = [];
        List<Type> interfaces = [];
        List<Type> genericConcretions = [];
        List<Type> genericInterfaces = [];

        List<Type> types = assembliesToScan
            .SelectMany(a => a.DefinedTypes)
            .Where(t => !t.ContainsGenericParameters || configuration.RegisterGenericHandlers)
            .Where(t => t.IsConcrete() && t.FindInterfacesThatClose(openRequestInterface).Any())
            .Where(configuration.TypeEvaluator)
            .ToList();

        foreach (Type type in types)
        {
            Type[] interfaceTypes = type.FindInterfacesThatClose(openRequestInterface).ToArray();
            bool isOpenGeneric = type.IsOpenGeneric();
            List<Type> concretionList = isOpenGeneric ? genericConcretions : concretions;
            List<Type> interfaceList = isOpenGeneric ? genericInterfaces : interfaces;

            concretionList.Add(type);
            foreach (Type? interfaceType in interfaceTypes)
            {
                interfaceList.Fill(interfaceType);
            }
        }

        return (concretions, interfaces, genericConcretions, genericInterfaces);
    }

    private static void RegisterExactMatches(Type @interface, List<Type> concretions, IServiceCollection services, bool addIfAlreadyExists)
    {
        List<Type> exactMatches = concretions.Where(x => x.CanBeCastTo(@interface)).ToList();

        if (addIfAlreadyExists)
        {
            foreach (Type type in exactMatches)
            {
                _ = services.AddTransient(@interface, type);
            }
        }
        else
        {
            if (exactMatches.Count > 1)
            {
                _ = exactMatches.RemoveAll(m => !IsMatchingWithInterface(m, @interface));
            }

            foreach (Type type in exactMatches)
            {
                services.TryAddTransient(@interface, type);
            }
        }

        if (!@interface.IsOpenGeneric())
        {
            AddConcretionsThatCouldBeClosed(@interface, concretions, services);
        }
    }

    private static bool IsMatchingWithInterface(Type? handlerType, Type handlerInterface)
    {
        if (handlerType == null || handlerInterface == null)
        {
            return false;
        }

        if (handlerType.IsInterface)
        {
            if (handlerType.GenericTypeArguments.SequenceEqual(handlerInterface.GenericTypeArguments))
            {
                return true;
            }
        }
        else
        {
            return IsMatchingWithInterface(handlerType.GetInterface(handlerInterface.Name), handlerInterface);
        }

        return false;
    }

    private static void AddConcretionsThatCouldBeClosed(Type @interface, List<Type> concretions, IServiceCollection services)
    {
        foreach (Type? type in concretions
                     .Where(x => x.IsOpenGeneric() && x.CouldCloseTo(@interface)))
        {
            try
            {
                services.TryAddTransient(@interface, type.MakeGenericType(@interface.GenericTypeArguments));
            }
            catch (Exception)
            {
                // The concretion cannot be closed to this interface (e.g. its generic
                // constraints are not satisfied), so there is nothing to register. Swallowing
                // the exception here is intentional: we simply skip this registration.
            }
        }
    }

    private static (Type Service, Type Implementation) GetConcreteRegistrationTypes(Type openRequestHandlerInterface, Type concreteGenericTRequest, Type openRequestHandlerImplementation)
    {
        Type[] closingTypes = concreteGenericTRequest.GetGenericArguments();

        Type? concreteTResponse = concreteGenericTRequest.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?.GetGenericArguments()
            .FirstOrDefault();

        Type typeDefinition = openRequestHandlerInterface.GetGenericTypeDefinition();

        Type serviceType = concreteTResponse != null ?
            typeDefinition.MakeGenericType(concreteGenericTRequest, concreteTResponse) :
            typeDefinition.MakeGenericType(concreteGenericTRequest);

        return (serviceType, openRequestHandlerImplementation.MakeGenericType(closingTypes));
    }

    private static List<Type>? GetConcreteRequestTypes(Type openRequestHandlerInterface, Type openRequestHandlerImplementation, IEnumerable<Assembly> assembliesToScan, CancellationToken cancellationToken)
    {
        //request generic type constraints       
        List<Type[]> constraintsForEachParameter = openRequestHandlerImplementation
            .GetGenericArguments()
            .Select(x => x.GetGenericParameterConstraints())
            .ToList();

        List<List<Type>> typesThatCanCloseForEachParameter = constraintsForEachParameter
            .Select(constraints => assembliesToScan
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && constraints.All(constraint => constraint.IsAssignableFrom(type))).ToList()
            ).ToList();

        Type requestType = openRequestHandlerInterface.GenericTypeArguments[0];

        if (requestType.IsGenericParameter)
        {
            return null;
        }

        Type requestGenericTypeDefinition = requestType.GetGenericTypeDefinition();

        List<List<Type>> combinations = GenerateCombinations(requestType, typesThatCanCloseForEachParameter, 0, cancellationToken);

        return [.. combinations.Select(types => requestGenericTypeDefinition.MakeGenericType([.. types]))];
    }

    // Method to generate combinations recursively
    public static List<List<Type>> GenerateCombinations(Type requestType, List<List<Type>> lists, int depth = 0, CancellationToken cancellationToken = default)
    {
        if (depth == 0)
        {
            ValidateCombinationsLimits(requestType, lists);
        }

        if (depth >= lists.Count)
        {
            return [[]];
        }

        cancellationToken.ThrowIfCancellationRequested();

        List<Type> currentList = lists[depth];
        List<List<Type>> childCombinations = GenerateCombinations(requestType, lists, depth + 1, cancellationToken);
        List<List<Type>> combinations = [];

        foreach (Type item in currentList)
        {
            foreach (List<Type> childCombination in childCombinations)
            {
                List<Type> currentCombination = [item, .. childCombination];
                combinations.Add(currentCombination);
            }
        }

        return combinations;
    }

    private static void ValidateCombinationsLimits(Type requestType, List<List<Type>> lists)
    {
        if (MaxGenericTypeParameters > 0 && lists.Count > MaxGenericTypeParameters)
        {
            throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The number of generic type parameters exceeds the maximum allowed ({MaxGenericTypeParameters}).");
        }

        foreach (List<Type> list in lists)
        {
            if (MaxTypesClosing > 0 && list.Count > MaxTypesClosing)
            {
                throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. One of the generic type parameter's count of types that can close exceeds the maximum length allowed ({MaxTypesClosing}).");
            }
        }

        // Calculate the total number of combinations
        long totalCombinations = 1;
        foreach (List<Type> list in lists)
        {
            totalCombinations *= list.Count;
            if (MaxGenericTypeParameters > 0 && totalCombinations > MaxGenericTypeRegistrations)
            {
                throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The total number of generic type registrations exceeds the maximum allowed ({MaxGenericTypeRegistrations}).");
            }
        }
    }

    private static void AddAllConcretionsThatClose(Type openRequestInterface, List<Type> concretions, IServiceCollection services, IEnumerable<Assembly> assembliesToScan, CancellationToken cancellationToken)
    {
        foreach (Type concretion in concretions)
        {
            List<Type>? concreteRequests = GetConcreteRequestTypes(openRequestInterface, concretion, assembliesToScan, cancellationToken);

            if (concreteRequests is null)
            {
                continue;
            }

            IEnumerable<(Type Service, Type Implementation)> registrationTypes = concreteRequests
                .Select(concreteRequest => GetConcreteRegistrationTypes(openRequestInterface, concreteRequest, concretion));

            foreach ((Type? Service, Type? Implementation) in registrationTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _ = services.AddTransient(Service, Implementation);
            }
        }
    }

    internal static bool CouldCloseTo(this Type openConcretion, Type closedInterface)
    {
        Type openInterface = closedInterface.GetGenericTypeDefinition();
        Type[] arguments = closedInterface.GenericTypeArguments;

        Type[] concreteArguments = openConcretion.GenericTypeArguments;
        return arguments.Length == concreteArguments.Length && openConcretion.CanBeCastTo(openInterface);
    }

    private static bool CanBeCastTo(this Type pluggedType, Type pluginType) => pluggedType != null && (pluggedType == pluginType || pluginType.IsAssignableFrom(pluggedType));

    private static bool IsOpenGeneric(this Type type)
        => type.IsGenericTypeDefinition || type.ContainsGenericParameters;

    internal static IEnumerable<Type> FindInterfacesThatClose(this Type pluggedType, Type templateType)
        => FindInterfacesThatClosesCore(pluggedType, templateType).Distinct();

    private static IEnumerable<Type> FindInterfacesThatClosesCore(Type pluggedType, Type templateType)
    {
        if (pluggedType == null)
        {
            yield break;
        }

        if (!pluggedType.IsConcrete())
        {
            yield break;
        }

        if (templateType.IsInterface)
        {
            foreach (
                Type? interfaceType in
                pluggedType.GetInterfaces()
                    .Where(type => type.IsGenericType && (type.GetGenericTypeDefinition() == templateType)))
            {
                yield return interfaceType;
            }
        }
        else if (pluggedType.BaseType!.IsGenericType &&
                 (pluggedType.BaseType!.GetGenericTypeDefinition() == templateType))
        {
            yield return pluggedType.BaseType!;
        }

        if (pluggedType.BaseType == typeof(object))
        {
            yield break;
        }

        foreach (Type interfaceType in FindInterfacesThatClosesCore(pluggedType.BaseType!, templateType))
        {
            yield return interfaceType;
        }
    }

    private static bool IsConcrete(this Type type)
        => !type.IsAbstract && !type.IsInterface;

    private static void Fill<T>(this IList<T> list, T value)
    {
        if (list.Contains(value))
        {
            return;
        }

        list.Add(value);
    }

    public static void AddRequiredServices(IServiceCollection services, MediatRServiceConfiguration serviceConfiguration)
    {
        // Use TryAdd, so any existing ServiceFactory/IMediator registration doesn't get overridden
        services.TryAdd(new ServiceDescriptor(typeof(IMediator), serviceConfiguration.MediatorImplementationType, serviceConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), serviceConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), serviceConfiguration.Lifetime));

        ServiceDescriptor notificationPublisherServiceDescriptor = serviceConfiguration.NotificationPublisherType != null
            ? new ServiceDescriptor(typeof(INotificationPublisher), serviceConfiguration.NotificationPublisherType, serviceConfiguration.Lifetime)
            : new ServiceDescriptor(typeof(INotificationPublisher), serviceConfiguration.NotificationPublisher);

        services.TryAdd(notificationPublisherServiceDescriptor);

        // Register pre processors, then post processors, then behaviors
        if (serviceConfiguration.RequestExceptionActionProcessorStrategy == RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions)
        {
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
        }
        else
        {
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
        }

        if (serviceConfiguration.RequestPreProcessorsToRegister.Count != 0)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(serviceConfiguration.RequestPreProcessorsToRegister);
        }

        if (serviceConfiguration.RequestPostProcessorsToRegister.Count != 0)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(serviceConfiguration.RequestPostProcessorsToRegister);
        }

        foreach (ServiceDescriptor serviceDescriptor in serviceConfiguration.BehaviorsToRegister)
        {
            services.TryAddEnumerable(serviceDescriptor);
        }

        foreach (ServiceDescriptor serviceDescriptor in serviceConfiguration.StreamBehaviorsToRegister)
        {
            services.TryAddEnumerable(serviceDescriptor);
        }
    }

    private static void RegisterBehaviorIfImplementationsExist(IServiceCollection services, Type behaviorType, Type subBehaviorType)
    {
        bool hasAnyRegistrationsOfSubBehaviorType = services
            .Where(service => !service.IsKeyedService)
            .Select(service => service.ImplementationType)
            .OfType<Type>()
            .SelectMany(type => type.GetInterfaces())
            .Where(type => type.IsGenericType)
            .Select(type => type.GetGenericTypeDefinition())
            .Any(type => type == subBehaviorType);

        if (hasAnyRegistrationsOfSubBehaviorType)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, ServiceLifetime.Transient));
        }
    }
}
