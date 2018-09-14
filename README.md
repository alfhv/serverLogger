# serverLogger

## For Client side:

### remoteAppender.csproj:
The remote appender DLL to be referenced on client'side.

## For Server side:

### serverLogger.csproj:
Implementation of server side logger with WebApi controller to receive calls from client.

### serverLogger.Service.csproj:
The WebApi host service to install on Windows.  

## Sample:

### clientLogger.csproj:
console application as example about how to log to server.

### serverLogger.Console.csproj:
console application self hosting WebApi implementation (serverLogger).

## Add the appender on client application

in log4net section of config file add the following appender.
NOTE: appender name should be exactly **name="RemoteLoggerAppender"**

```
    <appender name="RemoteLoggerAppender" type="remoteAppender.RemoteLoggerAppender, remoteAppender">
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%-5p %d   [%-2thread] %method: %m%n"/>
        </layout>
    </appender>
```

in app.config, add **loggerServerUrl** key. The value should be the URL of ServerLogger WebApi instance.
```
  <appSettings>
    <add key="loggerServerUrl" value="http://msvbrc01.cib.net:9000" />
  </appSettings>
```  

If loggerServerUrl key not found or server is not reachable in supplied URL or faulted respond received, then the appender will softly shutdown (no exception throw in client side), it wont be called again, and a WARN log line will be generate on client side logs.
