using Xunit;

namespace Aurora.Server.Tests;

public class ManagerTests
{
    [Fact]
    public void CommandManagerRegistersAndExecutesCommand()
    {
        var manager = new CommandManager();
        bool executed = false;
        
        manager.RegisterCommand("help", args => executed = true);
        
        bool result = manager.ExecuteCommand("help 1 2 3");
        
        Assert.True(result);
        Assert.True(executed);
    }
    
    [Fact]
    public void TickManagerAdvancesTick()
    {
        var manager = new TickManager();
        
        Assert.Equal(0, manager.CurrentTick);
        manager.AdvanceTick();
        Assert.Equal(1, manager.CurrentTick);
    }
}
