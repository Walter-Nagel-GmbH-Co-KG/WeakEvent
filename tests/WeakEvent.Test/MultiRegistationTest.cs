using System.ComponentModel;
using System.Diagnostics.Metrics;
using NUnit.Framework;
using WeakEvent;
using static DevExpress.Xpo.Helpers.PerformanceCounters;

namespace WN.Utils.Test;

[TestFixture]
public class MultiRegistationTest
{
    protected WeakEventSource<String> propertyChanged = new WeakEventSource<String>();

    public event EventHandler<String>? PropertyChanged
    {
        add { propertyChanged.Subscribe(this, value.ConvertDelegate<EventHandler<String>>()); }
        remove { propertyChanged.Unsubscribe(this, value.ConvertDelegate<EventHandler<String>>()); }
    }

    public event EventHandler<String>? PropertyChangedUniqueRegistration
    {
        add { propertyChanged.Subscribe(this, value.ConvertDelegate<EventHandler<String>>(), true); }
        remove { propertyChanged.Unsubscribe(this, value.ConvertDelegate<EventHandler<String>>()); }
    }

    private ManualResetEventSlim propertyChangedEvent = new ManualResetEventSlim();
    private Int32 counter = 0;

    [SetUp]
    public void SetUp()
    {
        counter = 0;
        propertyChanged = new WeakEventSource<String>();
    }

    [Test]
    public void OneRegistation()
    {

        PropertyChanged += MultiRegistationTest_PropertyChanged;

        propertyChanged.Raise(this, "abc");
        propertyChangedEvent.Wait(1000);
        Assert.AreEqual(1, counter);
    }

    private void MultiRegistationTest_PropertyChanged(Object? sender, String e)
    {
        Assert.AreEqual("abc", e);
        counter++;
        propertyChangedEvent.Set();
    }

    [Test]
    public void twoRegistation()
    {

        PropertyChanged += MultiRegistationTest_PropertyChanged;
        PropertyChanged += MultiRegistationTest_PropertyChanged;

        propertyChanged.Raise(this, "abc");
        propertyChangedEvent.Wait(1000);
        propertyChangedEvent.Wait(1000);
        Assert.AreEqual(2, counter);
    }

    [Test]
    public void twoRegistationUniqueRegistration()
    {

        PropertyChangedUniqueRegistration += MultiRegistationTest_PropertyChanged;
        PropertyChangedUniqueRegistration += MultiRegistationTest_PropertyChanged;

        propertyChanged.Raise(this, "abc");
        propertyChangedEvent.Wait(1000);
        Thread.Sleep(1000);
        Assert.AreEqual(1, counter);
    }

    [Test]
    public void OneRegistationUniqueRegistration()
    {

        PropertyChangedUniqueRegistration += MultiRegistationTest_PropertyChanged;

        propertyChanged.Raise(this, "abc");
        propertyChangedEvent.Wait(1000);

        Thread.Sleep(1000);
        Assert.AreEqual(1, counter);
    }

    [Test]
    public void degistationUniqueRegistration()
    {

        PropertyChangedUniqueRegistration += MultiRegistationTest_PropertyChanged;
        PropertyChangedUniqueRegistration += MultiRegistationTest_PropertyChanged;
        PropertyChangedUniqueRegistration -= MultiRegistationTest_PropertyChanged;

        propertyChanged.Raise(this, "abc");
        propertyChangedEvent.Wait(1000);

        Thread.Sleep(1000);
        Assert.AreEqual(0, counter);
    }
    [Test]
    public void degistation()
    {

        PropertyChangedUniqueRegistration += MultiRegistationTest_PropertyChanged;
        PropertyChangedUniqueRegistration -= MultiRegistationTest_PropertyChanged;

        propertyChanged.Raise(this, "abc");
        propertyChangedEvent.Wait(1000);

        Thread.Sleep(1000);
        Assert.AreEqual(0, counter);
    }

    [Test]
    public void degistationNotExisting()
    {

        PropertyChangedUniqueRegistration -= MultiRegistationTest_PropertyChanged;

        propertyChanged.Raise(this, "abc");
        propertyChangedEvent.Wait(1000);

        Thread.Sleep(1000);
        Assert.AreEqual(0, counter);
    }
}
