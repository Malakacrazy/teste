using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Statistics and metrics for EventStore performance monitoring
    /// </summary>
    [Serializable]
    public class EventStoreStatistics
    {
        /// <summary>
        /// Total number of events processed since creation
        /// </summary>
        public int TotalEventsProcessed { get; set; }
        
        /// <summary>
        /// Current number of events stored in memory
        /// </summary>
        public int CurrentEventCount { get; set; }
        
        /// <summary>
        /// Number of snapshots created
        /// </summary>
        public int SnapshotCount { get; set; }
        
        /// <summary>
        /// Current memory usage in bytes
        /// </summary>
        public long MemoryUsageBytes { get; set; }
        
        /// <summary>
        /// Number of different event types being tracked
        /// </summary>
        public int EventTypeCount { get; set; }
        
        /// <summary>
        /// Number of different players being tracked
        /// </summary>
        public int PlayerCount { get; set; }
        
        /// <summary>
        /// Last time a snapshot was created
        /// </summary>
        public DateTime LastSnapshotTime { get; set; }
        
        /// <summary>
        /// Last time events were persisted to disk
        /// </summary>
        public DateTime LastPersistenceTime { get; set; }
        
        /// <summary>
        /// Memory usage in megabytes (convenience property)
        /// </summary>
        public double MemoryUsageMB => MemoryUsageBytes / (1024.0 * 1024.0);
        
        /// <summary>
        /// Events per second processing rate (rough estimate)
        /// </summary>
        public double EventsPerSecond
        {
            get
            {
                var elapsed = DateTime.UtcNow - CreationTime;
                return elapsed.TotalSeconds > 0 ? TotalEventsProcessed / elapsed.TotalSeconds : 0;
            }
        }
        
        /// <summary>
        /// Time since last snapshot in seconds
        /// </summary>
        public double SecondsSinceLastSnapshot => (DateTime.UtcNow - LastSnapshotTime).TotalSeconds;
        
        /// <summary>
        /// Time since last persistence in seconds
        /// </summary>
        public double SecondsSinceLastPersistence => (DateTime.UtcNow - LastPersistenceTime).TotalSeconds;
        
        /// <summary>
        /// Average events per snapshot
        /// </summary>
        public double AverageEventsPerSnapshot => SnapshotCount > 0 ? (double)TotalEventsProcessed / SnapshotCount : 0;
        
        /// <summary>
        /// Storage efficiency (events in memory vs total processed)
        /// </summary>
        public double StorageEfficiency => TotalEventsProcessed > 0 ? (double)CurrentEventCount / TotalEventsProcessed : 1.0;
        
        /// <summary>
        /// When these statistics were created
        /// </summary>
        public DateTime CreationTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Get performance rating based on current metrics
        /// </summary>
        /// <returns>Performance rating from Poor to Excellent</returns>
        public PerformanceRating GetPerformanceRating()
        {
            var score = 0;
            
            // Memory usage score (lower is better)
            if (MemoryUsageMB < 50) score += 25;
            else if (MemoryUsageMB < 100) score += 20;
            else if (MemoryUsageMB < 200) score += 15;
            else if (MemoryUsageMB < 500) score += 10;
            else score += 5;
            
            // Events per second score
            if (EventsPerSecond > 100) score += 25;
            else if (EventsPerSecond > 50) score += 20;
            else if (EventsPerSecond > 20) score += 15;
            else if (EventsPerSecond > 10) score += 10;
            else score += 5;
            
            // Storage efficiency score
            if (StorageEfficiency > 0.8) score += 25;
            else if (StorageEfficiency > 0.6) score += 20;
            else if (StorageEfficiency > 0.4) score += 15;
            else if (StorageEfficiency > 0.2) score += 10;
            else score += 5;
            
            // Snapshot frequency score
            if (SecondsSinceLastSnapshot < 60) score += 25;
            else if (SecondsSinceLastSnapshot < 120) score += 20;
            else if (SecondsSinceLastSnapshot < 300) score += 15;
            else if (SecondsSinceLastSnapshot < 600) score += 10;
            else score += 5;
            
            return score switch
            {
                >= 90 => PerformanceRating.Excellent,
                >= 80 => PerformanceRating.VeryGood,
                >= 70 => PerformanceRating.Good,
                >= 60 => PerformanceRating.Fair,
                >= 50 => PerformanceRating.Poor,
                _ => PerformanceRating.Critical
            };
        }
        
        /// <summary>
        /// Get formatted statistics summary
        /// </summary>
        /// <returns>Statistics summary string</returns>
        public string GetSummary()
        {
            return $"EventStore Stats - Events: {TotalEventsProcessed} total / {CurrentEventCount} in memory, " +
                   $"Memory: {MemoryUsageMB:F1}MB, Snapshots: {SnapshotCount}, " +
                   $"Performance: {EventsPerSecond:F1} events/sec, Rating: {GetPerformanceRating()}";
        }
        
        /// <summary>
        /// Get detailed statistics report
        /// </summary>
        /// <returns>Detailed report string</returns>
        public string GetDetailedReport()
        {
            var report = $"EventStore Detailed Statistics Report\n";
            report += $"=====================================\n";
            report += $"Total Events Processed: {TotalEventsProcessed:N0}\n";
            report += $"Events in Memory: {CurrentEventCount:N0}\n";
            report += $"Memory Usage: {MemoryUsageMB:F2} MB ({MemoryUsageBytes:N0} bytes)\n";
            report += $"Snapshots Created: {SnapshotCount}\n";
            report += $"Event Types Tracked: {EventTypeCount}\n";
            report += $"Players Tracked: {PlayerCount}\n";
            report += $"Events per Second: {EventsPerSecond:F2}\n";
            report += $"Storage Efficiency: {StorageEfficiency:P1}\n";
            report += $"Average Events per Snapshot: {AverageEventsPerSnapshot:F1}\n";
            report += $"Last Snapshot: {SecondsSinceLastSnapshot:F0} seconds ago\n";
            report += $"Last Persistence: {SecondsSinceLastPersistence:F0} seconds ago\n";
            report += $"Performance Rating: {GetPerformanceRating()}\n";
            report += $"Uptime: {(DateTime.UtcNow - CreationTime).TotalMinutes:F1} minutes\n";
            
            return report;
        }
        
        /// <summary>
        /// Check if any metrics indicate potential issues
        /// </summary>
        /// <returns>True if there are potential performance issues</returns>
        public bool HasPerformanceIssues()
        {
            return MemoryUsageMB > 500 ||
                   EventsPerSecond < 1 ||
                   StorageEfficiency < 0.1 ||
                   SecondsSinceLastSnapshot > 1800; // 30 minutes
        }
        
        /// <summary>
        /// Get recommendations for improving performance
        /// </summary>
        /// <returns>List of recommendations</returns>
        public string[] GetPerformanceRecommendations()
        {
            var recommendations = new System.Collections.Generic.List<string>();
            
            if (MemoryUsageMB > 500)
            {
                recommendations.Add("High memory usage detected. Consider reducing MaxStoredEvents or enabling more aggressive cleanup.");
            }
            
            if (EventsPerSecond < 1)
            {
                recommendations.Add("Low event processing rate. Check for performance bottlenecks in event handlers.");
            }
            
            if (StorageEfficiency < 0.1)
            {
                recommendations.Add("Low storage efficiency. Most events are being cleaned up quickly. Consider adjusting cleanup intervals.");
            }
            
            if (SecondsSinceLastSnapshot > 1800)
            {
                recommendations.Add("Snapshots are infrequent. Consider reducing snapshot interval for better recovery capability.");
            }
            
            if (SnapshotCount == 0 && TotalEventsProcessed > 100)
            {
                recommendations.Add("No snapshots created yet. Ensure snapshot creation is enabled and functioning.");
            }
            
            if (recommendations.Count == 0)
            {
                recommendations.Add("Performance looks good! No specific recommendations at this time.");
            }
            
            return recommendations.ToArray();
        }
        
        /// <summary>
        /// Convert to string representation
        /// </summary>
        /// <returns>Summary string</returns>
        public override string ToString()
        {
            return GetSummary();
        }
    }
    
    /// <summary>
    /// Performance rating levels for EventStore
    /// </summary>
    public enum PerformanceRating
    {
        Critical,
        Poor,
        Fair,
        Good,
        VeryGood,
        Excellent
    }
}