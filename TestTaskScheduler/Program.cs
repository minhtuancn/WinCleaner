using System;
using System.Reflection;
using System.IO;
using Microsoft.Win32.TaskScheduler;

class Program
{
    static void Main()
    {
        // Create a WeeklyTrigger and check its properties
        var trigger = new WeeklyTrigger();
        Console.WriteLine($"WeeklyTrigger type: {trigger.GetType().FullName}");
        
        var props = trigger.GetType().GetProperties();
        foreach (var prop in props)
        {
            if (prop.Name.Contains("Day", StringComparison.OrdinalIgnoreCase) || 
                prop.Name.Contains("Week", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  Property: {prop.Name} - Type: {prop.PropertyType.FullName}");
            }
        }
        
        // Check if there's a DaysOfWeek property
        var daysOfWeekProp = trigger.GetType().GetProperty("DaysOfWeek");
        if (daysOfWeekProp != null)
        {
            Console.WriteLine($"DaysOfWeek property type: {daysOfWeekProp.PropertyType.FullName}");
            var value = daysOfWeekProp.GetValue(trigger);
            Console.WriteLine($"Default value: {value}");
        }
        
        // Also check DailyTrigger
        var dailyTrigger = new DailyTrigger();
        Console.WriteLine($"\nDailyTrigger type: {dailyTrigger.GetType().FullName}");
    }
}