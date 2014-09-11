PerfIt!
======

Windows performance monitoring for ASP.NET Web API actions

FAQ
===

**What is PerfIt?**

PerfIt is a performance counter publishing tool for ASP.NET Web API. By a little bit of setup, it *painlessly* publishes standard performance counters.

**Why should I use it?**

If you have a serious project exposing an API, you would want to monitor performance of your API - and possibly tracking many other aspects of your application. PerfIt makes it painlessly easy to do so.

**What counters does it publish?**

There are 4 standard counters that come with PerfIt out of the box (`TotalNoOfOperations`, `AverageTimeTaken`, `LastOperationExecutionTime`, `NumberOfOperationsPerSecond`) and you can choose one or all (typically you would choose **all**).

You can also create your own counters by implementing a simple base class.

**What is the overhead of using PerfIt?**

It is negligible compared to the code you would normally find within an API. It is within 1-2 ms.

**Can I use it with ASP.NET Web API 2?**

Yes, you can use it with any version of the ASP.NET Web API. There is a problem (that has a workaround) when registering Web API 2 which is an inherent problem with the `InstallUtil.exe` which does not honour `AssemblyRedirect` and the workaround has been discussed below.

Getting Started
==

### Step 1: Create an ASP.NET Web API

Use Visual Studio to create an ASP.NET Web API and add a controller (such as `DefaultController`).

### Step 2: Add PerfIt to your project

In the package manager console, type:

```
PM> Install-Package PerfIt
```

### Step 3: Add PerfIt delegating handler

In the `WebApiConfig` class when setting up the configuration, add a PerfIt delegating handler:

``` C#
config.MessageHandlers.Add(new PerfItDelegatingHandler());
```
### Step 4: Decorate your actions

In the actions you would want to monitor, add this attribute:

``` C#
[PerfItFilter(Description = "Gets all items",
            Counters = new[]{
            CounterTypes.TotalNoOfOperations,
            CounterTypes.AverageTimeTaken, 
            CounterTypes.NumberOfOperationsPerSecond,
            CounterTypes.LastOperationExecutionTime})]
public string Get()
{
    ...
}
```

### Step 5: Add an installer class

Right-click on your ASP.NET Web API and choose Add an Item and then from the menu choose "Installer Class".

Once added, click F7 to see the code and then add these lines:

``` C#
public override void Install(IDictionary stateSaver)
{
    base.Install(stateSaver);
    PerfItRuntime.Install(Assembly.GetExecutingAssembly()));
}

public override void Uninstall(IDictionary savedState)
{
    base.Uninstall(savedState);
    PerfItRuntime.Uninstall(Assembly.GetExecutingAssembly()));
}
```

### Step 6: Register your counters using InstallUtil.exe

In the command line, change directory to the .NET framework in use and then use -i switch to install ASP.NET Web API dll:

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319>InstallUtil -i "c:\myproject\path\bin\MyAspNetWebApi.dll"
```

### Step 7: Use your API and see the counters being published

Your counters will be published under a category, with the same name as your project. Underneath, you will see instance(s) of your individual counters.

![counters](https://raw.githubusercontent.com/aliostad/PublicImages/master/etc/Perfit.png)

Common scenarios
===

### Changing default category name

You can do this by supplying your custom category name to **all three** methods below (make sure they are all the same):

``` C#
PerfItRuntime.Install("MyCategoryName");

// AND

PerfItRuntime.Uninstall("MyCategoryName");

// AND

var handler = new PerfItDelegatingHandler("MyCategoryName");

```

### Changing default instance name

By default, instance name is composed of controller name and action name. If you have two actions with the same name (overloading a method), either you need to use `ActionName` attribute or change the method name (e.g. instead of `Get` using `GetCustomer`).

An alternative method is to supply instance name (also useful when you want to supply your custom name):

``` C#
[PerfItFilter(Description = "Gets all items",
    Counters = ..., 
    InstanceName="AnyName")]
public string Get()
{
    ...
}
```

### Turn off publishing counters

For whatever reason you might decide to turn off publishing counters. All you have to do is to add this entry to the AppSetting of your web.config:

``` XML
  <appSettings>
    ...
    <add key="perfit:publishCounters" value="false"/>
    ...
  </appSettings>

```
### Not to throw publishing errors

By default, publishing performance counters are regarded as having the same importance as the application's business logic and all publishing errors are thrown. If you would like to change this behaviour, you can do so both in code or in config:

```
PerfItRuntime.ThrowPublishingErrors = false;

```

Or by configuration:

``` XML
  <appSettings>
    ...
    <add key="perfit:publishErrors" value="false"/>
    ...
  </appSettings>

```
### FileNotFoundException when registering the DLL using InstallUtil.exe

A common problem is to encounter `FileNotFoundException` when registering your counters using `InstallUtil`. This is more common when your use Web API 2. In any case, this is a problem with InstallUtil not honouring your assembly redirects. To solve the problem (and it is just a workaround), simply copy the assembly redirect directives to `InstallUtil.exe.config`, run the installation and then remove them.

This has been raised a few times (see the issues, for example [#11](https://github.com/aliostad/PerfIt/issues/11)) but the problem is simply the way InstallUtil works - or rather doesn't.

What I found the best solution is to include a copy of InstallUtil.exe and its custom config (which works for your project by copying content if assemblyBinding section of the web.config) along with your deployables and have a script to install the counter, rather than relying on the standard InstallUtil on the box. These files are small and certainly a good solution.
