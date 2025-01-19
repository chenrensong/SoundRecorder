using System;
using System.Collections.Generic;

namespace SoundRecorder.Models
{
    public class Resume
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CandidateName { get; set; }
        public string PdfPath { get; set; }
        public string PdfContent { get; set; }
        public DateTime ImportedAt { get; set; }
    }

    public class Position
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> RequiredSkills { get; set; } = new();
    }

    public class InterviewQuestion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Question { get; set; }
        public bool WasAsked { get; set; }
        public int UsageCount { get; set; }
        public double EffectivenessScore { get; set; } // 基于面试反馈的效果评分
        public string PositionId { get; set; }
        public List<string> RelatedSkills { get; set; } = new();
    }

    public class Interview
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ResumeId { get; set; }
        public string PositionId { get; set; }
        public DateTime InterviewDate { get; set; }
        public List<InterviewQuestion> Questions { get; set; } = new();
        public string RecordingId { get; set; }
        public string Notes { get; set; }
        public InterviewStatus Status { get; set; }
    }

    public enum InterviewStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled
    }
} 