# Speckle Desktop UI
A desktop UI for Speckle Desktop Connectors 🎉

**NOTE**: this depends on Core branch `izzy/avatars`

## Trying it out
The UI will run on it's own, but it will do pretty much nothing. You can have a look around though to get a feel for it. It uses `DummyBindings` which you can edit if you want to dig a bit deeper.

## Implementing your own connector
You'll need to implement the `ConnectorBindings` for your application to allow the UI to interact with it. The Revit Connector is a good place to look for reference.

To start the UI from within your appliation, you'll need to create an instance of the UI `Bootstrapper` and give it an instance of your bindings. You then just need to call `Setup` on the `Bootstrapper` and you're on your way 🚀

### basic example

```cs
// create a new bindings instance
var bindings = new MyConnectorBindings();

// give it to the bootstrapper
var bootstrapper = new Bootstrapper()
{
  Bindings = bindings
};

// fire it up, baby!
bootstrapper.Setup(Application.Current);

```