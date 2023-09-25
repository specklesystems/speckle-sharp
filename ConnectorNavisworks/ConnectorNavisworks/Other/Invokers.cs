using System;
using System.Reflection;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using Speckle.ConnectorNavisworks.Bindings;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.ConnectorNavisworks.Other;

public class Invoker
{
  private readonly Control _control = ConnectorBindingsNavisworks.Control;

  internal object Invoke(Delegate method, params object[] args)
  {
    try
    {
      return InvokeOnUIThreadWithException(_control, method, args);
    }
    catch (TargetInvocationException ex)
    {
      // log the exception or re-throw it
      throw ex.InnerException ?? ex;
    }
  }

  private static object InvokeOnUIThreadWithException(Control control, Delegate method, object[] args)
  {
    if (control == null)
      return null;

    object result = null;

    control.Invoke(
      new Action(() =>
      {
        try
        {
          result = method?.DynamicInvoke(args);
        }
        catch (TargetInvocationException ex)
        {
          // log the exception or re-throw it
          throw ex.InnerException ?? ex;
        }
      })
    );

    return result;
  }
}

/// <summary>
/// Handles the invocation of progress operations in the UI thread.
/// Implements IDisposable to ensure the proper release of resources.
/// </summary>
public sealed class ProgressInvoker : Invoker
{
  private readonly Progress _progressBar;

  /// <summary>
  /// Initializes a new instance of the ProgressInvoker class.
  /// Provides an instance of the Progress class which starts reporting of progress
  /// of an operation, typically displaying a progress bar or dialog to the end user.
  /// </summary>
  /// <param name="title">The title of the progress operation. Defaults to an empty string.</param>
  public ProgressInvoker(Progress applicationProgress)
  {
    _progressBar = applicationProgress;
  }

  public bool IsCanceled => _progressBar.IsCanceled;

  /// <summary>
  /// Starts a sub operation. Can be nested.
  /// </summary>
  /// <param name="fractionOfRemainingTime">Progress in sub op between 0 and 1 maps to fraction of remaining time in main operation.</param>
  /// <param name="message">The message of the sub operation. Defaults to an empty string.</param>
  internal void BeginSubOperation(double fractionOfRemainingTime, string message = "")
  {
    Invoke(new Action<double, string>(_progressBar.BeginSubOperation), fractionOfRemainingTime, message);
  }

  /// <summary>
  /// Ends the current sub operation.
  /// Identifies that a sub operation, which is part of a larger operation, has completed.
  /// </summary>
  internal void EndSubOperation()
  {
    Update(1.0);    
    Invoke(new Action(_progressBar.EndSubOperation));
  }

  /// <summary>
  /// Where a sub operation is not nested, this method can be used to start a new sub operation.
  /// </summary>
  /// <param name="fractionOfRemainingTime"></param>
  /// <param name="message"></param>
  internal void StartNewSubOperation(double fractionOfRemainingTime, string message = "")
  {
    EndSubOperation();
    BeginSubOperation(fractionOfRemainingTime, message);
  }

  /// <summary>
  /// Updates progress on the current operation or sub-operation
  /// Nominally the API call returns false if user wants to cancel operation,
  /// true if they want to continue.
  /// We are ignoring this for now.
  /// </summary>
  /// <param name="fractionDone">Fraction of operation or sub-operation completed, between 0 and 1.</param>
  internal void Update(double fractionDone)
  {
    Func<double, bool> updateProgress = _progressBar.Update;

    Invoke(updateProgress, Math.Min(fractionDone, 1));
  }

  /// <summary>
  /// Forces operation to cancel next time Update is called
  /// </summary>
  public void Cancel()
  {
    if (!_progressBar.IsDisposed)
      Invoke(new Action(_progressBar.Cancel), null);
  }
}

/// <summary>
/// Handles the invocation of object conversions in the UI thread.
/// </summary>
public class ConversionInvoker : Invoker
{
  private readonly Func<object, Base> _convertToSpeckle;

  /// <summary>
  /// Initializes a new instance of the ConversionInvoker class.
  /// </summary>
  /// <param name="converter">The SpeckleConverter to use for object conversion.</param>
  public ConversionInvoker(ISpeckleConverter converter)
  {
    _convertToSpeckle = converter.ConvertToSpeckle;
  }

  /// <summary>
  /// Converts an input object to a Speckle Base object.
  /// </summary>
  /// <param name="navisworksObject">The object to convert. This can be a ModelItem, View or whichever conversions get added.</param>
  /// <returns>A Speckle Base object.</returns>
  public Base Convert(object navisworksObject)
  {
    return (Base)Invoke(_convertToSpeckle, navisworksObject);
  }
}
