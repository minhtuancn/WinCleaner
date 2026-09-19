using System;
using TaskScheduler;

class Program
{
    static void Main()
    {
        // Check if DaysOfWeek exists in TaskScheduler namespace
        var type = Type.GetType("TaskScheduler.DaysOfWeek, TaskScheduler");
        if (type != null)
        {
            Console.WriteLine("Found: TaskScheduler.DaysOfWeek");
            foreach (var name in Enum.GetNames(type))
            {
                Console.WriteLine($"  {name}");
            }
        }
        else
        {
            Console.WriteLine("NOT FOUND: TaskScheduler.DaysOfWeek");
        }
        
        // Also check Microsoft.Win32.TaskScheduler
        var type2 = Type.GetType("Microsoft.Win32.TaskScheduler.DaysOfWeek, TaskScheduler");
        if (type2 != null)
        {
            Console.WriteLine("Found: Microsoft.Win32.TaskScheduler.DaysOfWeek");
            foreach (var name in Enum.GetNames(type2))
            {
                Console.WriteLine($"  {name}");
            }
        }
        else
        {
            Console.WriteLine("NOT FOUND: Microsoft.Win32.TaskScheduler.DaysOfWeek");
        }
    }
}