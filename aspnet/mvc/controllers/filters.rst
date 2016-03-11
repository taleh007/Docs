Filters
=======

By `Steve Smith`_

*Filters* in ASP.NET MVC allow you to run code before or after a particular stage in the execution pipeline. Filters can be configured globally, per-controller, or per-action.

.. contents:: Sections
    :local:
    :depth: 1

`View or download sample from GitHub <https://github.com/aspnet/Docs/tree/master/mvc/controllers/filters/sample>`_.

How do filters work?
--------------------

Each kind of filter will be executed at a different stage in the pipeline, and thus has its own set of intended scenarios. Choose what kind of filter to create based on the task you need it to perform, and where in the request pipeline it executes. Filters run within the MVC Action Invocation Pipeline, sometimes referred to as the *Filter Pipeline*, which runs after MVC selects the action to execute.

.. image:: filters/_static/filter-pipeline-1.png

Different kinds of filters run at different points within the pipeline. Some filters, like authorization filters, only run before the next stage in the pipeline, and take no action afterward. Other filters, like action filters, can execute both before and after other parts of the pipeline execute, as shown below.

.. image:: filters/_static/filter-pipeline-2.png

Which filter do I need?
^^^^^^^^^^^^^^^^^^^^^^^

:ref:`Authorization filters <authorization-filters>` are used to determine whether the current user is authorized for the request being made.

:ref:`Resource filters <resource-filters>` are the first filter to handle a request after authorization, and the last one to touch the request as it is leaving the filter pipeline. They're especially useful to implement caching or otherwise short-circuit the pipeline for performance reasons.

:ref:`Action filters <action-filters>` wrap calls to individual action method calls, and can manipulate the arguments passed into an action as well as the response returned from it.

:ref:`Exception filters <exception-filters>` are used to apply global policies to unhandled exceptions in the MVC app.

:ref:`Result filters <result-filters>` wrap the execution of individual action results, but only when no exceptions have been thrown. They are ideal for logic that must surround view execution or formatter execution.

Implementation
^^^^^^^^^^^^^^

All filters support both synchronous and asynchronous implementations through different interface definitions. Choose the sync or async variant depending on the kind of task you need to perform. They are interchangeable from the framework's perspective.

Synchonous filters define both an On\ *Stage*\ Executing and On\ *Stage*\ Executed method (with noted exceptions). The On\ *Stage*\ Executing method will be called before the event pipeline stage by the Stage name, and the On\ *Stage*\ Executed method will be called after the pipeline stage named by the Stage name.

.. literalinclude:: filters/sample/src/FiltersSample/Filters/SampleActionFilter.cs
  :language: c#
  :emphasize-lines: 6,8,13

Asynchronous filters define a single On\ *Stage*\ ExecutionAsync method that will surround execution of the pipeline stage named by Stage. The On\ *Stage*\ ExecutionAsync method is provided a *Stage*\ ExecutionDelegate delegate which will execute the pipeline stage named by Stage when invoked and awaited.

.. literalinclude:: filters/sample/src/FiltersSample/Filters/SampleAsyncActionFilter.cs
  :language: c#
  :emphasize-lines: 6,8-9

In most cases, you can implement both the async and synchronous interfaces with the same class, like so:

.. literalinclude:: filters/sample/src/FiltersSample/Filters/SampleCombinedActionFilter.cs
  :language: c#
  :emphasize-lines: 11-12

Filters and Attributes
^^^^^^^^^^^^^^^^^^^^^^
It's often convenient to implement filter interfaces as *Attributes*. These attributes are then used to apply the filter to certain controllers or action methods. The framework includes built-in attribute-based filters that you can subclass and customize. For example, the following filter inherits from `ResultFilterAttribute <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/Filters/ResultFilterAttribute/index.html>`_, and overrides its ``OnResultExecuting`` method to add a header to the response.

.. _add-header-attribute:

.. literalinclude:: filters/sample/src/FiltersSample/Filters/AddHeaderAttribute.cs
  :language: c#
  :emphasize-lines: 5,16

Attributes allow filters to accept arguments, as shown in the example above. You would add this attribute to a controller or action method and specify the name and value of the HTTP header you wished to add to the response:

.. literalinclude:: filters/sample/src/FiltersSample/Controllers/SampleController.cs
  :language: c#
  :emphasize-lines: 1
  :lines: 6-12,25
  :dedent: 4

The result of the ``Index`` action is shown below - the response headers are displayed on the bottom right.

.. image:: filters/_static/add-header.png

The available built-in attribute-based filters are:
	- `ActionFilterAttribute <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/Filters/ActionFilterAttribute/index.html>`_
	- `AuthorizationFilterAttribute <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/Filters/AuthorizationFilterAttribute/index.html>`_
	- `ExceptionFilterAttribute <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/Filters/ExceptionFilterAttribute/index.html>`_
	- `ResultFilterAttribute <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/Filters/ResultFilterAttribute/index.html>`_

Dependency Injection
^^^^^^^^^^^^^^^^^^^^
Filters are not directly returned from :doc:`dependency injection </fundamentals/dependency-injection>` (DI). Filters that are also attributes, and which are added directly to controller classes or action methods cannot have constructor dependencies provided by DI - their constructor properties must be specified when they are declared. This is a limitation of how attributes work. However, if your filters have dependencies you need to access from DI, there are several supported approaches. You can apply your filter using either the `ServiceFilter <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/ServiceFilterAttribute/index.html>`_ or `TypeFilter <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/TypeFilterAttribute/index.html>`_ attribute, or you can implement IFilterFactory on your attribute, or you can access the services you require from the context parameter provided to your filter's On\ *Stage* methods.

For an example of this last technique, consider the following method, which needs to access a service that has been registered with DI. The service is accessible from the context parameter:

.. literalinclude:: filters/sample/src/FiltersSample/Filters/OrderLoggingActionFilterAttribute.cs
  :language: c#
  :emphasize-lines: 3-4
  :lines: 10-20
  :dedent: 8

Cancellation and Short Circuiting
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
You can short-circuit the filter pipeline at any point by setting the ``Result`` property on the context parameter provided to the filter method. For instance, the following ``ShortCircuitingResourceFilter`` will prevent any other filters from running later in the pipeline, including any action filters.

.. _short-circuiting-resource-filter:

.. literalinclude:: filters/sample/src/FiltersSample/Filters/ShortCircuitingResourceFilterAttribute.cs
  :language: c#
  :emphasize-lines: 12-15

When this filter is applied to an action that also would have an action filter applied (later in the pipeline), such as the ``SomeResource`` action below, the action filter is not executed (in this case the ``AddHeader`` action filter is applied at the class level, but the behavior would be the same if it were applied to the method).

.. literalinclude:: filters/sample/src/FiltersSample/Controllers/SampleController.cs
  :language: c#
  :emphasize-lines: 1,4
  :lines: 6-8, 14-19
  :dedent: 4 

Configuring Filters
-------------------
Filters can be *scoped* at three different levels. You can add a particular filter to a particular action as an attribute. You can add a filter to all actions within a controller by applying an attribute at the controller level. Or you can register a filter globally, to be run with every MVC action.

Filters are added globally in ``Startup``, when configuring MVC:

.. literalinclude:: filters/sample/src/FiltersSample/Startup.cs
  :language: c#
  :emphasize-lines: 3-7
  :lines: 15-24
  :dedent: 8

Filters can be added by type, or an instance can be added. If you add an instance, that instance will be used for every request. If you add a type, the instance will be created through DI, and any constructor dependencies will be populated by DI. In the example above, the :ref:`DurationActionFilter <duration-action-filter>` has a dependency on ``ILoggerFactory`` in its constructor, which is fulfilled by DI on each request.

If you have a filter you don't want to be scoped globally, which requires dependencies to be provided through DI, you can apply it at the constructor or action level using the ``ServiceFilter`` or ``TypeFilter`` attribute. ``ServiceFilter`` lets you specify, as an attribute, the type of a filter you want to apply. It will instantiate the filter and any dependencies it has using DI, as shown in this example:

.. literalinclude:: filters/sample/src/FiltersSample/Controllers/HomeController.cs
  :language: c#
  :emphasize-lines: 1
  :lines: 19-24
  :dedent: 8

If you try to use ``ServiceFilter`` without registering the filter type in ``ConfigureServices``, you will get an exception like this one: *System.InvalidOperationException: No service for type 'FiltersSample.Filters.LoggingAddHeaderFilter' has been registered.* You can see that this type was registered in code example above - this is the important line:

.. literalinclude:: filters/sample/src/FiltersSample/Startup.cs
  :language: c#
  :emphasize-lines: 1
  :lines: 21
  :dedent: 12

``ServiceFilterAttribute`` implements `IFilterFactory <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/Filters/IFilterFactory/index.html>`_, which exposes a single method for creating an ``IFilter`` instance. In the case of ServiceFilter, this is implemented by using the built-in service provider to load the type from DI.

``TypeFilterAttribute`` is very similar to ``ServiceFilterAttribute`` (and also implements ``IFilterFactory``), but its type is not resolved directly from the DI container. Instead, it instantiates the type using an `ObjectFactory delegate <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/Extensions/DependencyInjection/ObjectFactory/index.html>`_. Because of this difference, types that are referenced using the ``TypeFilterAttribute`` do not need to be registered with the container first (but they will still have their dependencies fulfilled by the container). Also, ``TypeFilterAttribute`` can optionally accept constructor arguments for the type in question. The following example demonstrates how to pass arguments to a type using ``TypeFilterAttribute``:

.. literalinclude:: filters/sample/src/FiltersSample/Controllers/HomeController.cs
  :language: c#
  :emphasize-lines: 1-2
  :lines: 33-39
  :dedent: 8

If you have a simple filter that doesn't require any arguments, but which has constructor dependencies that need to be filled by DI, you can inherit from ``TypeFilterAttribute``, allowing you to use your own named attribute on classes and methods (instead of ``[TypeFilter(typeof(FilterType))]``). The following filter shows how this can be implemented:

.. literalinclude:: filters/sample/src/FiltersSample/Filters/SampleLoggingActionFilterAttribute.cs
  :language: c#
  :emphasize-lines: 1, 3, 7
  :lines: 8-38
  :dedent: 4

This filter can be applied to classes or methods using the ``[SampleLoggingActionFilter]`` syntax.

.. note:: Some of the filters shown in these examples implement logging in order to demonstrate when, where, or how they are executing. You should avoid creating and using filters purely for logging purposes, since the :doc:`built-in framework logging features </fundamentals/logging>` should already provide what you need to see how MVC is working under the covers. If you're going to add logging to your filters, it should focus on business domain concerns or behavior specific to your filter, rather than MVC actions or other framework events.

``IFilterFactory`` actually implements ``IFilter``, so anywhere in the filter pipeline an ``IFilterFactory`` can be used in place of an ``IFilter`` instance. When it comes time for a filter to run, it is cast to an ``IFilterFactory``. If that cast succeeds, then the ``CreateInstance`` method is called to create the actual ``IFilter`` that will be used. This provides a very flexible design, since the precise filter pipeline does not need to be set in stone when the application starts.

You can implement ``IFilterFactory`` on your own attribute implementations as another approach to creating filters:

.. literalinclude:: filters/sample/src/FiltersSample/Filters/AddHeaderWithFactoryAttribute.cs
  :language: c#
  :emphasize-lines: 1,3-6
  :lines: 6-25
  :dedent: 4

@rynowak - can you clarify this next bit?
If you're using ``IFilterFactory``, you can specify lifetime of the filter (how?). When your filter is the instance, your filter instance is cached, so avoid do anything stateful in the filter returned by ``CreateInstance``.
 
Ordering
^^^^^^^^
Filters can be applied per-action method (via attribute) or via controller (via attribute), or in global filters collection. Scope also generally determines ordering. The filter closest to the action runs first; generally you get overriding behavior without having to explicitly set ordering. This is sometimes referred to as "russian doll" nesting, as each increase in scope is wrapped around the previous scope, like a `nesting doll <https://en.wikipedia.org/wiki/Matryoshka_doll>`_.

In addition to scope, filters can override their sequence of execution by implementing `IOrderedFilter <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/Filters/IOrderedFilter/index.html>`_. This interface simply exposes an ``int`` ``Order`` property, and filters execute in ascending numeric order based on this property. All of the built-in filters, including ``TypeFilterAttribute`` and ``ServiceFilterAttribute``, implement ``IOrderedFilter``, so you can specify the order of filters when you apply the attribute to a class or method.

To see the scope-based ordering in action, consider the following class, which has the same filter applied at both the class and method level. The same filter is also registered globally. When run, the log output will show the order in which the filters executed, and how these compared to the ``Controller`` base class's ``OnActionExecuting`` and ``OnActionExecuted`` methods.

.. literalinclude:: filters/sample/src/FiltersSample/Controllers/OrderingController.cs
  :language: c#
  :emphasize-lines: 1,10,18,24
  :lines: 8-34
  :dedent: 4

The resulting order is:
	1. The Controller ``OnActionExecuting``
	2. The Global filter ``OnActionExecuting``
	3. The Class filter ``OnActionExecuting``
	4. The Method filter ``OnActionExecuting``
	5. The Method filter ``OnActionExecuted``
	6. The Class filter ``OnActionExecuted``
	7. The Global filter ``OnActionExecuted``
	8. The Controller ``OnActionExecuted``

.. note:: ``IOrderedFilter`` trumps scope when determining the order in which filters will run. Filters are sorted first by order, then scope is used to break ties. Order defaults to 0 if not set.

To modify the default, scope-based order, you could explicitly set the ``Order`` property of the class-level and/or method-level filters. For example, ``[OrderLoggingActionFilter(Name = "Method Level Attribute", Order=-1)]``. In this case, a value of less than zero would ensure this filter ran before both the Global and Class level filters.

The new order would be:
	#. The Controller ``OnActionExecuting``
	#. The Method filter ``OnActionExecuting``
	#. The Global filter ``OnActionExecuting``
	#. The Class filter ``OnActionExecuting``
	#. The Class filter ``OnActionExecuted``
	#. The Global filter ``OnActionExecuted``
	#. The Method filter ``OnActionExecuted``
	#. The Controller ``OnActionExecuted``

.. _authorization-filters:

Authorization Filters
---------------------
*Authorization Filters* control access to action methods, and are the first filters to be executed within the filter pipeline. They have only a before stage, unlike most filters that support before and after methods. You should only write a custom authorization filter if you are writing your own authorization framework. Note that you should not throw exceptions within authorization filters, since nothing will handle the exception (exception filters won't handle them). Instead, issue a challenge or find another way. They are covered in the :doc:`Security </security/index>` section of the documentation.

Learn more about :doc:`/security/authorization/authorization-filters`.

.. _resource-filters:

Resource Filters
----------------
*Resource Filters* implement either the ``IResourceFilter`` or ``IAsyncResourceFilter`` interface, and their execution wraps most of the filter pipeline (only :ref:`authorization-filters` run before them). Resource filters are expecially useful if you need to short-circuit most of the work a request is doing. Caching would be one example use case for a resource filter, since if the response is already in the cache, the filter can immediately set a result and avoid the rest of the processing for the action.

The :ref:`short circuiting resource filter <short-circuiting-resource-filter>` shown above is one example of a resource filter. A very naive cache implementation (do not use this in production) is shown below:

.. literalinclude:: filters/sample/src/FiltersSample/Filters/NaiveCacheResourceFilterAttribute.cs
  :language: c#
  :emphasize-lines: 1-2,11,16,24,26
  :lines: 8-36
  :dedent: 4

In ``OnResourceExecuting``, if the result is already in the static dictionary cache, the ``Result`` property is set on ``context``, and the action short-circuits and returns with the cached result. In the ``OnResourceExecuted`` method, if the current request's key isn't already in use, the current ``Result`` is stored in the cache, to be used by future requests.

Adding this filter to a class or method is shown here:

.. literalinclude:: filters/sample/src/FiltersSample/Controllers/CachedController.cs
  :language: c#
  :emphasize-lines: 1-2,6
  :lines: 7-14
  :dedent: 4
  
.. _action-filters:

Action Filters
--------------
*Action Filters* implement either the ``IActionFilter`` or ``IAsyncActionFilter`` interface and their execution surrounds the execution of action methods. Action filters are ideal for any logic that needs to see the results of model binding, or modify the controller or inputs to an action method. Additionally, action filters can view and directly modify the result of an action method.

The ``OnActionExecuting`` method runs before the action method, so it can manipulate the inputs to the action by changing ``ActionExecutingContext.ActionArguments`` or manipulate the controller through ``ActionExecutingContext.Controller``. An ``OnActionExecuting`` method can short-circuit execution of the action method and subsequent action filters by setting ``ActionExecutingContext.Result``. Throwing an exception in an ``OnActionExecuting`` method will also prevent execution of the action method and subsequent filters, but will be treated as a failure instead of successful result.

The ``OnActionExecuted`` method runs after the action method, and can see and manipulate the results of the action through the ``ActionExecutedContext.Result`` property. ``ActionExecutedContext.Canceled`` will be set to true if the action execution was short-circuited by another filter. ``ActionExecutedContext.Exception`` will be set to a non-null value if the action or a subsequent action filter threw an exception. Setting ``ActionExecutedContext.Exception`` to null effectively 'handles' an exception, and ``ActionExectedContext.Result`` will then be executed as if it were returned from the action method normally.

For an ``IAsyncActionFilter`` the ``OnActionExecutionAsync`` combines all the possibilites of ``OnActionExecuting`` and ``OnActionExecuted``. A call to ``await next()`` on the ``ActionExecutionDelegate`` will execute any subsequent action filters and the action method, returning an ``ActionExecutedContext``. To short-circuit inside of an ``OnActionExecutionAsync``, set ``ActionExecutingContext.Result`` and do not call the ``ActionExectionDelegate``.

The ``DurationActionFilter`` shown below uses a ``Stopwatch`` to measure the execution time of the action, for display on the page. The ``ViewData`` property of the ``ViewResult`` can be accessed and manipulated, including adding new values.

.. _duration-action-filter:

.. literalinclude:: filters/sample/src/FiltersSample/Filters/DurationActionFilter.cs
  :language: c#
  :emphasize-lines: 1,21-26
  :lines: 8-35
  :dedent: 4

.. note:: If you just need to log how long an action takes to execute, rather than displaying it on the page, this is already done by the framework.

.. _exception-filters:

Exception Filters
-----------------
*Exception Filters* implement either the ``IExceptionFilter`` or ``IAsyncExceptionFilter`` interface.

Exception filters handle unhandled exceptions. They are only called when an exception occurs later in the pipeline. They can provide a single location to implement common error handling policies within an app. The framework provides an abstract `ExceptionFilterAttribute <https://docs.asp.net/projects/api/en/latest/autoapi/Microsoft/AspNet/Mvc/Filters/ExceptionFilterAttribute/index.html>`_ that you should be able to subclass for your needs. Exception filters are good for trapping exceptions that occur within MVC actions, but they're not as flexible as error handling middleware. Prefer middleware for the general case, and use filters only where the middleware doesn't offer the functionality your app requires.

Exception filters do not have two events (for before and after) - they only implement ``OnException`` (or ``OnExceptionAsync``). The ``ExceptionContext`` provided in the ``OnException`` parameter includes the ``Exception`` that occurred. If you set ``context.Exception`` to null, the effect is that you've handled the exception, so the request will proceed as if it hadn't occurred (most likely returning a 200 OK status). The following filter logs exceptions and marks them as handled:

.. literalinclude:: filters/sample/src/FiltersSample/Filters/LoggingExceptionFilterAttribute.cs
  :language: c#
  :emphasize-lines: 7,15,18

.. _result-filters:

Result Filters
--------------
*Result Filters* implement either the ``IResultFilter`` or ``IAsyncResultFilter`` interface and their execution surrounds the execution of action results. Result filters are only executed for successful results - when the action or action filters produce an action result. **Result filters are not executed when exception filters handle an exception.**

Result filters are ideal for any logic that needs to directly surround view execution or formatter execution. Result filters can replace or modify the action result that's responsible for producing the response.

The ``OnResultExecuting`` method runs before the action result is executed, so it can manipulate the action result through ``ResultExecutingContext.Result``. An ``OnResultExecuting`` method can short-circuit execution of the action result and subsequent result filters by setting ``ResultExecutingContext.Cancel`` to true. If short-circuited, MVC will not modify the response; take care to write to the response object directly when short-circuiting. Throwing an exception in an ``OnResultExecuting`` method will also prevent execution of the action result and subsequent filters, but will be treated as a failure instead of a successful result.

The ``OnResultExecuted`` method runs after the action result has executed. At this point if no exception was thrown, the response has likely been sent to the client and cannot be changed further. ``ResultExecutedContext.Canceled`` will be set to true if the action result execution was short-circuited by another filter. ``ResultExecutedContext.Exception`` will be set to a non-null value if the action result or a subsequent result filter threw an exception. Setting ``ResultExecutedContext.Exception`` to null effectively 'handles' an exception and will prevent the exeception from being rethrown by MVC later in the pipeline. If handling an exception in a result filter, consider whether or not it's appropriate to write any data to the response. The action result may have thrown partway through its execution, and if the headers have already been flushed to the client there's no proper way to send a failure status code, unfortunately.

For an ``IAsyncResultFilter`` the ``OnResultExecutionAsync`` combines all the possibilites of ``OnResultExecuting`` and ``OnResultExecuted``. A call to ``await next()`` on the ``ResultExecutionDelegate`` will execute any subsequent result filters and the action result, returning a ``ResultExecutedContext``. To short-circuit inside of an ``OnResultExecutionAsync``, set ``ResultExecutingContext.Cancel`` to true and do not call the ``ResultExectionDelegate``.

You can override the built-in ``ResultFilterAttribute`` to create result filters. The :ref:`AddHeaderAttribute <add-header-attribute>` class shown above is an example of a resource filter.

.. tip:: If you need to add headers to the response, it's best to do so before the action result executes. Otherwise, the response will most likely already have been sent to the client, and it will be too late to modify it. For a result filter, this means adding the header in ``OnResultExecuting`` rather than ``OnResultExecuted``.

Filters vs. Middleware
----------------------
In general, filters are meant to handle cross-cutting business and application concerns. This is often the same use case for :doc:`middleware </fundamentals/middleware>`. Filters are very similar to middleware in capability, but let you scope that behavior and insert it into a location in your app where it makes sense, such as before a view, or after model binding. Filters are a part of MVC, and have access to its context and constructs. For instance, middleware can't easily detect whether model validation on a request has generated errors, and respond accordingly, but a filter can easily do so.
