namespace Dinosaur.SqlServerToMySql
{
    internal static class Exceptive
    {
        public static readonly string[] Tables = { "wf_workflowdefinition", "wf_activity", "wf_transition", "wf_workflowapp", "wf_workflowapprelation", "wf_workflowinstance", "wf_activityinstance", "wf_transitioninstance", "wf_taskinstance", "wf_workflowinstancecomment" };
        public static readonly string ExTables = @"SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for wf_workflowdefinition
-- ----------------------------
DROP TABLE IF EXISTS `wf_workflowdefinition`;
CREATE TABLE `wf_workflowdefinition`  (
  `WorkflowId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '工作流表单ID',
  `WorkflowName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `IsCurrent` tinyint(1) NOT NULL DEFAULT 0,
  `Version` int(11) NOT NULL DEFAULT 1,
  `Remark` varchar(3000) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  `WorkflowMap` varchar(200) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `DefaultShowFlowMap` tinyint(1) NULL DEFAULT 0,
  `Creator` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '提交人',
  `CreatedTime` datetime NOT NULL,
  `LastModTime` datetime NULL DEFAULT NULL,
  `LastModifier` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkFlowDescription` text CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `AreaDescription` text CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `IsAttachment` tinyint(1) NOT NULL DEFAULT 1,
  `IsComment` tinyint(1) NOT NULL DEFAULT 1,
  `IsFormApproval` int(11) NOT NULL DEFAULT 0,
  `IsEnableReject` tinyint(1) NOT NULL DEFAULT 0,
  `IsHistoryActorPriority` tinyint(1) NOT NULL DEFAULT 0,
  `IsEnableWithdraw` tinyint(1) NOT NULL DEFAULT 0,
  `IsEnableInvalid` tinyint(1) NOT NULL DEFAULT 0,
  `IsEnableTransmit` int(11) NOT NULL DEFAULT 0,
  `IsWorkflowCirculation` int(11) NOT NULL DEFAULT 0,
  `WorkflowFormPath` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `IsFilterOrg` int(11) NULL DEFAULT NULL,
  PRIMARY KEY (`WorkflowId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for wf_activity
-- ----------------------------
DROP TABLE IF EXISTS `wf_activity`;
CREATE TABLE `wf_activity`  (
  `ActivityId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '工作流表单ID',
  `StepId` int(11) NOT NULL DEFAULT 1 COMMENT '步骤ID',
  `ActivityName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `ActivityShowName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `IsActivityDelete` tinyint(1) NOT NULL DEFAULT 0,
  `ActivityType` varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'Normal',
  `IsEditeForm` tinyint(1) NOT NULL DEFAULT 1,
  `IsOpinion` tinyint(1) NOT NULL DEFAULT 1,
  `IsMustAddOpinion` tinyint(1) NOT NULL DEFAULT 0,
  `ActorParser` varchar(200) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ActorParamter` text CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `AlertUser` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `AlertRule` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `DealHours` float NULL DEFAULT NULL,
  `ExpirationRule` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ExtendedProperty` text CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `Descriptions` varchar(300) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `EnterType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'manual',
  `OutType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'manual',
  `JoinType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'JoinXOR',
  `SplitType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'JoinXOR',
  `JoinRuleParam` text CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `SplitRuleParam` text CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `RespondType` varchar(10) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'anyone',
  `AllowTransmit` tinyint(1) NULL DEFAULT 0,
  `AssistUser` varchar(1024) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `CreatedTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Creator` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '提交人',
  `LastModTime` datetime NULL DEFAULT NULL,
  `LastActor` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `IsUserSelectedAll` tinyint(1) NOT NULL DEFAULT 0,
  `IsUserRadio` tinyint(1) NOT NULL DEFAULT 0,
  `NotifyType` int(11) NULL DEFAULT NULL,
  `RejectType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `IsNotifyStartor` tinyint(1) NULL DEFAULT 0,
  `MessageTemplate` varchar(1000) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `CirculatedActor` text CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `ExtendExcuteAssembly` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `PostionX` int(11) NOT NULL DEFAULT 0,
  `PostionY` int(11) NOT NULL DEFAULT 0,
  `Width` int(11) NOT NULL DEFAULT 100 COMMENT '宽度',
  `Height` int(11) NOT NULL DEFAULT 30,
  `AutoOutAssembly` varchar(800) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `CirculatedType` tinyint(4) NOT NULL DEFAULT 0,
  `IsShowFormApproval` tinyint(1) NOT NULL DEFAULT 1,
  `IsActivityCirculation` int(11) NOT NULL DEFAULT 1,
  `SubAppId` varchar(10) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `SubWorkflowAssembly` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ActivityFormPath` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `IsTheHistoryActorPriority` tinyint(1) NOT NULL DEFAULT 1,
  `IsTheEnableWithdraw` tinyint(1) NOT NULL DEFAULT 1,
  `IsTheEnableInvalid` tinyint(1) NOT NULL DEFAULT 1,
  `IsActorSpread` tinyint(1) NOT NULL DEFAULT 1,
  `ActivityCode` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `RejectActivity` text CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `IsFilterOrg` int(11) NULL DEFAULT NULL,
  `ProcessType` int(11) NULL DEFAULT NULL,
  PRIMARY KEY (`ActivityId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci KEY_BLOCK_SIZE = 8 ROW_FORMAT = COMPRESSED;

-- ----------------------------
-- Table structure for wf_transition
-- ----------------------------
DROP TABLE IF EXISTS `wf_transition`;
CREATE TABLE `wf_transition`  (
  `TransitionId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '工作流表单ID',
  `FromActivityId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `ToActivityId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `IsJoinActor` tinyint(1) NOT NULL DEFAULT 0,
  `TransitionName` varchar(200) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `TransitionCondition` text CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `Remark` varchar(200) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `CreatedTime` datetime NOT NULL,
  `Creator` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '提交人',
  `LineType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '电路类型',
  `PostionM` decimal(18, 2) NULL DEFAULT NULL,
  PRIMARY KEY (`TransitionId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for wf_workflowapp
-- ----------------------------
DROP TABLE IF EXISTS `wf_workflowapp`;
CREATE TABLE `wf_workflowapp`  (
  `AppId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `AppName` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Description` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '描述',
  `AppType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ShowOrder` float NULL DEFAULT 1,
  `AliasImageUrl` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `FormType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT '1' COMMENT '表单类别',
  `FormDefinitionId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `FormDefinitionPath` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `Enable` tinyint(1) NULL DEFAULT 1,
  `Creator` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL COMMENT '提交人',
  `CreatedTime` datetime NOT NULL,
  `Permissions` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `SheetNumberFormatExpression` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT '0',
  `SheetNumberCycle` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `OtherParameter` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `IsPowerEnable` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`AppId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for wf_workflowapprelation
-- ----------------------------
DROP TABLE IF EXISTS `wf_workflowapprelation`;
CREATE TABLE `wf_workflowapprelation`  (
  `AppId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '工作流表单ID',
  `CreatedTime` datetime NOT NULL,
  `ApprelationId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `IsCurrentApp` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`AppId`, `WorkflowId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for wf_workflowinstance
-- ----------------------------
DROP TABLE IF EXISTS `wf_workflowinstance`;
CREATE TABLE `wf_workflowinstance`  (
  `WorkflowInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `OpenBizDate` varchar(10) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '工作流表单ID',
  `AppId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `StartedTime` datetime NOT NULL,
  `FinishedTime` datetime NULL DEFAULT NULL,
  `SheetId` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `FormId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `WorkflowInstanceState` int(11) NOT NULL DEFAULT 0,
  `CreatorId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Creator` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '提交人',
  `CreatorRealName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `CreatorDepartId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `CreatorDpName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `WorkflowTitle` varchar(200) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Secrecy` int(11) NOT NULL DEFAULT 0,
  `Urgency` tinyint(4) NULL DEFAULT 0,
  `Importance` tinyint(4) NULL DEFAULT NULL,
  `ExpectFinishedTime` datetime NULL DEFAULT NULL,
  `Requirement` varchar(1000) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `CommentCount` int(11) NULL DEFAULT NULL,
  `ExtStr` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `MainWorkflowInstanceId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `UrgeTimes` tinyint(4) NOT NULL DEFAULT 0,
  PRIMARY KEY (`WorkflowInstanceId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for wf_activityinstance
-- ----------------------------
DROP TABLE IF EXISTS `wf_activityinstance`;
CREATE TABLE `wf_activityinstance`  (
  `ActivityInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `OpenBizDate` varchar(10) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '工作流表单ID',
  `AppId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `ActivityId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `StepId` int(11) NULL DEFAULT NULL COMMENT '步骤ID',
  `ActivityType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `OperateType` int(11) NULL DEFAULT 0,
  `CreatedTime` datetime NOT NULL,
  `FinishedTime` datetime NULL DEFAULT NULL,
  `Actor` varchar(1000) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `Command` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ExternalEntityType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ExternalEntityId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ActorDescription` varchar(1000) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `RespondType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `ActivityState` tinyint(4) NOT NULL DEFAULT 0,
  `ActivityRemark` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `SubWorkflowInstanceId` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`ActivityInstanceId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for wf_transitioninstance
-- ----------------------------
DROP TABLE IF EXISTS `wf_transitioninstance`;
CREATE TABLE `wf_transitioninstance`  (
  `TransitionInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `FromActivityInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `FromActivityId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `CreatedTime` datetime NOT NULL,
  `ToActivityId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `ToActivityInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `TransitionState` tinyint(4) NOT NULL DEFAULT 0,
  PRIMARY KEY (`TransitionInstanceId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for wf_taskinstance
-- ----------------------------
DROP TABLE IF EXISTS `wf_taskinstance`;
CREATE TABLE `wf_taskinstance`  (
  `TaskId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '任务ID ',
  `FromTaskId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `OpenBizDate` varchar(10) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `AppId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '工作流表单ID',
  `StepId` int(11) NOT NULL COMMENT '步骤ID',
  `TaskSeq` varchar(400) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `UserId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `UserName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `RealName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `UserDpId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `UserDpName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `DelegatorUserId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `DelegatorRealName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `DelegatorName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `DelegatorDpId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `DelegatorDpName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ActivityInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `ActivityId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `ActivityName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `ActivityShowName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `TaskState` int(11) NOT NULL DEFAULT 0,
  `CompletedType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `TaskCreateType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT '0',
  `RespondType` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `IsCompleter` tinyint(1) NOT NULL DEFAULT 0,
  `IsDelegatorCompleted` tinyint(1) NOT NULL DEFAULT 0,
  `IsContainDelegator` tinyint(1) NOT NULL DEFAULT 0,
  `Opinion` varchar(500) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `RealTime` datetime NULL DEFAULT NULL,
  `CompletedTime` datetime NULL DEFAULT NULL,
  `CreatedTime` datetime NOT NULL,
  `IsValid` tinyint(1) NULL DEFAULT 1,
  `IsCirculated` tinyint(1) NOT NULL DEFAULT 0,
  `FromCreator` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `FromCreatorID` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `IsReferred` tinyint(1) NULL DEFAULT 0,
  `TaskDealHours` float NOT NULL DEFAULT 0,
  `TaskRemark` varchar(1000) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `TaskExpireTime` datetime NULL DEFAULT NULL,
  `Mobile` varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ApprovalResult` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`TaskId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for wf_workflowinstancecomment
-- ----------------------------
DROP TABLE IF EXISTS `wf_workflowinstancecomment`;
CREATE TABLE `wf_workflowinstancecomment`  (
  `Id` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT 'ID',
  `WorkflowInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `OpenBizDate` varchar(10) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `WorkflowId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '工作流表单ID',
  `AppId` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `ActivityInstanceId` char(36) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `Message` varchar(4000) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '日志信息',
  `Creator` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '提交人',
  `CreatedTime` datetime NOT NULL,
  `ActivityName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ExtStr1` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ExtStr2` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ExtStr3` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `FK_WF_WorkflowInstanceComment_WorkflowInstance`(`WorkflowInstanceId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = DYNAMIC;

SET FOREIGN_KEY_CHECKS = 1;
";

        public static readonly string ExConstraints = @"ALTER TABLE `wf_activity` ADD CONSTRAINT `FK_Actitity_WorkflowDefinition` FOREIGN KEY (`WorkflowId`) REFERENCES `wf_workflowdefinition` (`WorkflowId`) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE `wf_transition` ADD CONSTRAINT `FK_TransitionActivity` FOREIGN KEY (`FromActivityId`) REFERENCES `wf_activity` (`ActivityId`) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE `wf_workflowapprelation` ADD CONSTRAINT `FK_wf_WorkflowApprelation_WorkflowApp` FOREIGN KEY (`AppId`) REFERENCES `wf_workflowapp` (`AppId`) ON DELETE RESTRICT ON UPDATE RESTRICT,ADD CONSTRAINT `FK_wf_WorkflowApprelation_WorkflowDefinition` FOREIGN KEY (`WorkflowId`) REFERENCES `wf_workflowdefinition` (`WorkflowId`) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE `wf_workflowinstancecomment` ADD CONSTRAINT `FK_WF_WorkflowInstanceComment_WorkflowInstance` FOREIGN KEY (`WorkflowInstanceId`) REFERENCES `wf_workflowinstance` (`WorkflowInstanceId`) ON DELETE RESTRICT ON UPDATE RESTRICT;

SET FOREIGN_KEY_CHECKS = 0;
-- ----------------------------
-- Triggers structure for table wf_workflowapprelation
-- ----------------------------
DROP TRIGGER IF EXISTS `wf_WorkflowApprelation_inserting`;

CREATE TRIGGER `wf_WorkflowApprelation_inserting` BEFORE INSERT ON `wf_workflowapprelation` FOR EACH ROW BEGIN

  IF (new.`ApprelationId` IS NULL) THEN
    SET new.`ApprelationId`=UUID();
  END IF;
END
;

-- ----------------------------
-- Triggers structure for table wf_transitioninstance
-- ----------------------------
DROP TRIGGER IF EXISTS `wf_TransitionInstance_inserting`;

CREATE TRIGGER `wf_TransitionInstance_inserting` BEFORE INSERT ON `wf_transitioninstance` FOR EACH ROW BEGIN

  IF (new.`ToActivityId` IS NULL) THEN
    SET new.`ToActivityId`=UUID();
  END IF;

  IF (new.`ToActivityInstanceId` IS NULL) THEN
    SET new.`ToActivityInstanceId`=UUID();
  END IF;

  IF (new.`WorkflowInstanceId` IS NULL) THEN
    SET new.`WorkflowInstanceId`=UUID();
  END IF;
END
;

SET FOREIGN_KEY_CHECKS = 1;";

        public static readonly Dictionary<string, string> ExIndies = new Dictionary<string, string>()
        {
            { "wf_activity", "ALTER TABLE `wf_activity` ADD INDEX `FK_Actitity_WorkflowDefinition`(`WorkflowId`) USING BTREE;" },
            { "wf_transition", "ALTER TABLE `wf_transition` ADD INDEX `FK_TransitionActivity`(`FromActivityId`) USING BTREE;" },
            { "wf_workflowapprelation", "ALTER TABLE `wf_workflowapprelation` ADD INDEX `FK_wf_WorkflowApprelation_WorkflowDefinition`(`WorkflowId`) USING BTREE;" },
            { "wf_workflowinstance", "ALTER TABLE `wf_workflowinstance` ADD INDEX `IX_AppId`(`AppId`) USING BTREE,ADD INDEX `IX_CreatorId`(`CreatorId`) USING BTREE,ADD INDEX `IX_WorkflowId`(`WorkflowId`) USING BTREE,ADD INDEX `IX_FormId`(`FormId`) USING BTREE;" },
            { "wf_activityinstance", "ALTER TABLE `wf_activityinstance` ADD INDEX `IX_WorkflowInstanceId`(`WorkflowInstanceId`, `ActivityState`) USING BTREE,ADD INDEX `IX_CreateTime`(`CreatedTime`) USING BTREE,ADD INDEX `IX_ActivityId`(`ActivityId`) USING BTREE;" },
            { "wf_transitioninstance", "ALTER TABLE `wf_transitioninstance` ADD INDEX `IX_WorkflowInstanceId`(`WorkflowInstanceId`) USING BTREE;" },
            { "wf_taskinstance", "ALTER TABLE `wf_taskinstance` ADD INDEX `IX_ActivityInstanceId`(`ActivityInstanceId`) USING BTREE,ADD INDEX `IX_CreateTime`(`CreatedTime`) USING BTREE,ADD INDEX `IX_WorkflowInstanceId`(`WorkflowInstanceId`, `TaskState`, `IsValid`, `IsCirculated`) USING BTREE,ADD INDEX `idx_Todo`(`UserId`, `TaskState`, `IsValid`, `IsCirculated`) USING BTREE;" }
        };
    }
}
