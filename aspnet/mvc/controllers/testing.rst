Testing Controller Logic
========================
By `Steve Smith`_

Controllers in ASP.NET MVC apps should be small and focused on user-interface concerns. Large controllers that deal with non-UI concerns are more difficult to test and maintain.

.. contents:: Sections
	:local:
	:depth: 1
	
`View sample files <https://github.com/aspnet/Docs/tree/master/aspnet/mvc/controllers/testing/sample>`_

What is Controller Logic
------------------------
*Controllers* define groups of related *actions*. The grouping of related actions into controllers is useful for :doc:`routing </fundamentals/routing>`, applying :doc:`filters <filters>`, and :doc:`injecting common dependencies <dependency-injection>`. 

:doc:`Learn more about controllers and actions <actions>`.

Controller logic should be minimal and not be focused on business logic or infrastructure concerns (for example, data access). Test controller logic, not the framework. Test how the controller *behaves* based on valid or invalid inputs. Test controller responses based on the result of the business operation it performs.

Typical controller responsibilities:
	- Verify ``ModelState.IsValid``
	- Return an error response if ``ModelState`` is invalid
	- Retrieve a business entity from persistence
	- Perform an action on the business entity
	- Save the business entity to persistence
	- Return an appropriate ``IActionResult``

Unit Testing
------------
:doc:`Unit testing </testing/unit-testing>` involves testing a part of an app in isolation from its infrastructure and dependencies. When unit testing controller logic, only the contents of a single action should be tested, not the behavior of its dependencies or of the framework itself. Thus, when unit testing a controller's actions, avoid testing global or attribute-based :doc:`filters <filters>`, :doc:`routing </fundamentals/routing>`, or :doc:`model binding </mvc/models/model-binding>`, as these are all performed by the framework (testing these is an integration test responsibility).

To demonstrate unit testing, review the following controller. It displays a list of brainstorming sessions and allows new brainstorming sessions to be created with a POST:

.. literalinclude:: testing/sample/TestingControllersSample/src/TestingControllersSample/Controllers/HomeController.cs
  :language: c#
  :emphasize-lines: 11,15,20,40-41

The controller is following the `explicit dependencies principle <http://deviq.com/explicit-dependencies-principle/>`_, expecting dependency injection to provide it with an instance of ``IBrainStormSessionRepository``. This makes it fairly easy to test using a mock object framework, like `Moq <https://www.nuget.org/packages/Moq/>`_. To test the first ``Index`` method, which has no looping or branching logic and only calls one method, we need only verify that a ``ViewResult`` is returned, with a ``ViewModel`` containing whatever was returned from the repository's ``List`` method.

.. literalinclude:: testing/sample/TestingControllersSample/tests/TestingControllerSample.Tests/UnitTests/HomeControllerIndex.cs
  :language: c#
  :emphasize-lines: 16-17,23-25

The second ``Index`` method (which accepts an ``HttpPost``) has slightly more logic, since it checks ``ModelState.IsValid``. We should test that, when ``ModelState.IsValid`` is ``false`` the action method returns a ``ViewResult`` with the appropriate data. Otherwise, we should confirm that the ``Add`` method is called on the repository and a ``RedirectToActionResult`` is returned with the correct arguments. Below are its unit tests:

.. literalinclude:: testing/sample/TestingControllersSample/tests/TestingControllerSample.Tests/UnitTests/HomeControllerIndexPost.cs
  :language: c#
  :emphasize-lines: 14-15,20,27-28,31,38

The first test confirms that, when the ``ModelState`` is not valid, the method returns the same ``ViewResult`` as a GET request would. Note that the test doesn't attempt to pass in an invalid model. That wouldn't work anyway since model binding isn't running - we're just calling the method directly. However, we're not trying to test model binding - we're only testing what our code in the action method does. So the simplest thing is to just explicitly add an error to ``ModelState``.

The second test verifies that when ``ModelState`` is valid, a new ``BrainStormSession`` is added (via the repository), and the method returns a ``RedirectToActionResult`` with the expected properties. Mocked calls that aren't called are normally ignored, but calling ``Verifiable`` at the end of the setup call allows it to be verified in the test. This is done with the call to ``mockRepo.Verify``.

Another controller in the app displays information related to a particular brainstorming session. It includes some logic to deal with invalid id values:

.. literalinclude:: testing/sample/TestingControllersSample/src/TestingControllersSample/Controllers/SessionController.cs
  :language: c#
  :emphasize-lines: 16,20,25,33

Looking at this controller action, it should be clear that there are at least three cases to test, one for each ``return`` statement. The unit tests are shown below:

.. literalinclude:: testing/sample/TestingControllersSample/tests/TestingControllerSample.Tests/UnitTests/SessionControllerIndex.cs
  :language: c#
  :emphasize-lines: 16,26,39

Finally, the application exposes some functionality as a web API, including a list of ideas associated with a brainstorming session and a method for adding new ideas to a session. The controller is shown below:

.. _ideas-controller:

.. literalinclude:: testing/sample/TestingControllersSample/src/TestingControllersSample/Api/IdeasController.cs
  :language: c#
  :emphasize-lines: 20-22,27,29-35,49-51,55,60,70

The ``ForSession`` method returns a list of ``dynamic`` types, with property names camel cased to match JavaScript conventions. Avoid returning your business domain entities directly via API calls, since frequently they include more data than the API client requires, and they unnecessarily couple your app's internal domain model with the API you expose externally. You can define strongly typed data-transfer objects (DTOs), or just return dynamic as this method does. Mapping between domain entities and the types you will return over the wire can be done manually (using a LINQ ``Select`` as shown here) or using a library like `AutoMapper <https://github.com/AutoMapper/AutoMapper>`_

.. note:: Returning ``dynamic`` types is a common practice, especially in web API methods, since it's a simple, easy way to control the shape of the output. However, ``dynamic`` types are ``internal``, so unit tests that attempt to refer to their properties may encounter errors unless the web project is configured to make its internals visible to the test project.

The unit tests for the ``ForSession`` API method is shown here:

.. literalinclude:: testing/sample/TestingControllersSample/tests/TestingControllerSample.Tests/UnitTests/ApiIdeasControllerForSession.cs
  :language: c#
  :emphasize-lines: 14-15,25-26,36-37

The second test, ``ReturnsIdeasForSession``, initially fails with this message:

.. code-block: c#

'object' does not contain a definition for 'name'

To correct this error, add an assembly directive specifying that the web project's internals should be visible to the test client. This directive can go into its own file (``InternalsVisibleTo.cs``):

.. literalinclude:: testing/sample/TestingControllersSample/src/TestingControllersSample/InternalsVisibleTo.cs
  :language: c#
  :emphasize-lines: 3

With this in place, the test will pass, since the unit test project will have access to the internal-scoped dynamic return type. This is only a problem for unit tests that directly inspect the result - integration tests do not require ``InternalsVisibleTo`` since they get a result from an ``HttpClient``, not a direct method return.

The unit tests for the ``Create`` method are shown here:

.. literalinclude:: testing/sample/TestingControllersSample/tests/TestingControllerSample.Tests/UnitTests/ApiIdeasControllerCreate.cs
  :language: c#
  :emphasize-lines: 13-14,18,23-24,28,35-36

Again, to test the behavior of the method when ``ModelState`` is invalid, the test adds a model error to the controller. Don't try to test model validation or model binding in your unit tests - just test your action method's behavior. The second test depends on the repository returning null, so the mock repository is configured to return null. There's no need to create a test database (in memory or otherwise) and construct a query that will return this result - it can be done in a single line as shown. The last test needs to verify that the repository's ``Update`` method is called, so once more this mock call is called with ``Verifiable`` and then the mocked repository's ``Verify`` method is called to confirm the verifiable method was executed as expected. It's not a unit test responsibility to ensure that the ``Update`` method really did save the data - that can be done with an integration test.

Integration Testing
-------------------
:doc:`Integration testing </testing/integration-testing>` is done to ensure separate modules within your app work correctly together. Generally, anything you can test with a unit test, you can also test with an integration test, but the reverse isn't true. However, integration tests tend to be much slower than unit tests. Thus, it's best to test whatever you can with unit tests, and use integration tests for scenarios that involve multiple collaborators.

Although they may still be useful, mock objects are rarely used in integration tests. In unit testing, mock objects are an effective way to control how collaborators outside of the unit being tested should behave for the purposes of the test. In an integration test, real collaborators are used, to confirm the whole subsystem works together correctly.

Application State
^^^^^^^^^^^^^^^^^
One important consideration when performing integration testing is how to set your app's state. Tests need to run independent of one another, and so each test should start with the app in a known state. If your app doesn't use a database or have any persistence, this is not an issue. However, most real-world apps persist their state to some kind of data store, so any modifications made by one test could impact another test unless the data store is reset. Using the built-in ``TestServer``, it's very straightforward to host ASP.NET Core apps within our integration tests, but that doesn't necessarily grant access to the data it will use. If you're using an actual database, one approach is to have the app point at a test database, which your tests can access and ensure is reset to a known state before each test executes.

In this sample application, I'm using Entity Framework Core's InMemoryDatabase support, so I can't just connect to it from my test project. Instead, I expose an ``InitializeDatabase`` method from the app's ``Startup`` class, which I call when the app starts up if it's in the ``Development`` environment. My integration tests automatically benefit from this as long as they set the environment to ``Development``. I don't have to worry about resetting the database, since the InMemoryDatabase is reset each time the app restarts.

The app's ``Startup`` class is shown here:

.. literalinclude:: testing/sample/TestingControllersSample/src/TestingControllersSample/Startup.cs
  :language: c#
  :emphasize-lines: 18-21,40-41,49,57

You'll see the ``GetTestSession`` method used frequently in the integration tests below.

Accessing Views
^^^^^^^^^^^^^^^
Each integration test class configures the ``TestServer`` that will run the ASP.NET Core app. By default, ``TestServer`` hosts the web app in the folder where it's running - in this case, the test project folder. Thus, when you attempt to test controller actions that return ``ViewResult``, you may see this error:

.. code-block:: c#

	The view 'Index' was not found. The following locations were searched:
	(list of locations)

To correct this issue, you need to configure the server to use the ``ApplicationBasePath`` and ``ApplicationName`` of the web project. This is done in the call to ``UseServices`` in the integration test class shown:

.. literalinclude:: testing/sample/TestingControllersSample/tests/TestingControllerSample.Tests/IntegrationTests/HomeControllerIndex.cs
  :language: c#
  :emphasize-lines: 20,22-32,37-38

In the test above, the ``responseString`` gets the actual rendered HTML from the View, which can be inspected to confirm it contains expected results.

API Methods
^^^^^^^^^^^
If your app exposes web APIs, it's a good idea to have automated tests confirm they execute as expected. The built-in ``TestServer`` makes this much easier to do than in previous versions of ASP.NET. If your API methods are using model binding, you should always check ``ModelState.IsValid``, and integration tests are the right place to confirm that your model validation is working properly. 

The following set of tests target the ``Create`` method in the :ref:`IdeasController <ideas-controller>` class shown above:

.. literalinclude:: testing/sample/TestingControllersSample/tests/TestingControllerSample.Tests/IntegrationTests/ApiIdeasControllerCreatePost.cs
  :language: c#
  :lines: 40-93
  :dedent: 8
  :emphasize-lines: 1-2,9-10,17-18,25-26,33-34,42-43,51

Unlike integration tests of actions that returns HTML views, web API methods that return results can usually be cast to strongly typed objects, as the last test above shows. In this case, the test casts the result to a ``BrainStormSession`` instance, and confirms that the idea was correctly added to its collection of ideas.

You'll find additional examples of integration tests in this article's `sample project <https://github.com/aspnet/Docs/tree/1.0.0-rc1/aspnet/mvc/controllers/testing/sample>`_.