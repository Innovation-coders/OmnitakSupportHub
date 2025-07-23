CREATE TABLE [Users] (
  [UserID] int PRIMARY KEY,
  [Email] varchar(255),
  [PasswordHash] varchar(128),
  [HashAlgorithm] varchar(20),
  [FullName] varchar(100),
  [RoleID] int,
  [TeamID] int,
  [DepartmentID] int,
  [CreatedAt] datetime
)
GO

CREATE TABLE [Roles] (
  [RoleID] int PRIMARY KEY,
  [RoleName] varchar(50)
)
GO

CREATE TABLE [Departments] (
  [DepartmentID] int PRIMARY KEY,
  [DepartmentName] varchar(50),
  [Description] varchar(255)
)
GO

CREATE TABLE [SupportTeams] (
  [TeamID] int PRIMARY KEY,
  [TeamName] varchar(100),
  [Description] varchar(255),
  [Specialization] varchar(100),
  [TeamLeadID] int
)
GO

CREATE TABLE [Tickets] (
  [TicketID] int PRIMARY KEY,
  [Title] varchar(200),
  [Description] text,
  [CreatedBy] int,
  [AssignedTo] int,
  [CategoryID] int,
  [StatusID] int,
  [PriorityID] int,
  [CreatedAt] datetime,
  [ClosedAt] datetime
)
GO

CREATE TABLE [Categories] (
  [CategoryID] int PRIMARY KEY,
  [CategoryName] varchar(50),
  [Description] varchar(255),
  [IsActive] boolean
)
GO

CREATE TABLE [Statuses] (
  [StatusID] int PRIMARY KEY,
  [StatusName] varchar(20),
  [IsActive] boolean
)
GO

CREATE TABLE [Priorities] (
  [PriorityID] int PRIMARY KEY,
  [PriorityName] varchar(50),
  [Label] varchar(20),
  [Weight] int,
  [IsActive] boolean
)
GO

CREATE TABLE [RoutingRules] (
  [RuleID] int PRIMARY KEY,
  [CategoryID] int,
  [TeamID] int,
  [Conditions] text,
  [IsActive] boolean
)
GO

CREATE TABLE [TicketTimeline] (
  [TimelineID] int PRIMARY KEY,
  [TicketID] int,
  [ExpectedResolution] datetime,
  [ActualResolution] datetime
)
GO

CREATE TABLE [ChatMessages] (
  [MessageID] int PRIMARY KEY,
  [TicketID] int,
  [UserID] int,
  [Message] text,
  [SentAt] datetime,
  [ReadAt] datetime
)
GO

CREATE TABLE [KnowledgeBase] (
  [ArticleID] int PRIMARY KEY,
  [Title] varchar(200),
  [Content] text,
  [CategoryID] int,
  [CreatedBy] int,
  [LastUpdatedBy] int,
  [CreatedAt] datetime,
  [UpdatedAt] datetime
)
GO

CREATE TABLE [Feedback] (
  [FeedbackID] int PRIMARY KEY,
  [TicketID] int,
  [UserID] int,
  [Rating] int,
  [Comment] varchar(500),
  [CreatedAt] datetime
)
GO

CREATE TABLE [AuditLogs] (
  [LogID] int PRIMARY KEY,
  [UserID] int,
  [Action] varchar(50),
  [TargetType] varchar(50),
  [TargetID] int,
  [Details] varchar(500),
  [PerformedAt] datetime
)
GO

CREATE TABLE [PasswordResets] (
  [Token] uuid PRIMARY KEY,
  [UserID] int,
  [ExpiresAt] datetime
)
GO

ALTER TABLE [Users] ADD FOREIGN KEY ([RoleID]) REFERENCES [Roles] ([RoleID])
GO

ALTER TABLE [Users] ADD FOREIGN KEY ([TeamID]) REFERENCES [SupportTeams] ([TeamID])
GO

ALTER TABLE [Users] ADD FOREIGN KEY ([DepartmentID]) REFERENCES [Departments] ([DepartmentID])
GO

ALTER TABLE [SupportTeams] ADD FOREIGN KEY ([TeamLeadID]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [Tickets] ADD FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [Tickets] ADD FOREIGN KEY ([AssignedTo]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [Tickets] ADD FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID])
GO

ALTER TABLE [Tickets] ADD FOREIGN KEY ([StatusID]) REFERENCES [Statuses] ([StatusID])
GO

ALTER TABLE [Tickets] ADD FOREIGN KEY ([PriorityID]) REFERENCES [Priorities] ([PriorityID])
GO

ALTER TABLE [RoutingRules] ADD FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID])
GO

ALTER TABLE [RoutingRules] ADD FOREIGN KEY ([TeamID]) REFERENCES [SupportTeams] ([TeamID])
GO

ALTER TABLE [TicketTimeline] ADD FOREIGN KEY ([TicketID]) REFERENCES [Tickets] ([TicketID])
GO

ALTER TABLE [ChatMessages] ADD FOREIGN KEY ([TicketID]) REFERENCES [Tickets] ([TicketID])
GO

ALTER TABLE [ChatMessages] ADD FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [KnowledgeBase] ADD FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID])
GO

ALTER TABLE [KnowledgeBase] ADD FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [KnowledgeBase] ADD FOREIGN KEY ([LastUpdatedBy]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [Feedback] ADD FOREIGN KEY ([TicketID]) REFERENCES [Tickets] ([TicketID])
GO

ALTER TABLE [Feedback] ADD FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [AuditLogs] ADD FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [PasswordResets] ADD FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
GO
