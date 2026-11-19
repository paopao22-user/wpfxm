-- Day49：AI 告警研判、维修建议与通知待办
-- 在已有 iot_gateway 数据库执行；不替代 Day31 的规则告警，也不向 alarms 表插入空规则。
USE iot_gateway;

-- 1. AI 告警研判表：一条记录 = 一次巡检证据的模型研判结论。
-- run_id 唯一索引保证同一巡检记录只研判一次。
CREATE TABLE IF NOT EXISTS ai_advisories (
  id CHAR(32) NOT NULL COMMENT '32 位无连字符 GUID',
  run_id CHAR(32) NOT NULL COMMENT '来源巡检任务 ID',
  owner VARCHAR(50) NOT NULL COMMENT '所有者，来自 JWT',
  device_code VARCHAR(50) NOT NULL,
  state VARCHAR(30) NOT NULL COMMENT 'Running/Active/NoAttention/Failed/Acknowledged/Closed',
  evidence_json LONGTEXT NOT NULL COMMENT '研判使用的真实证据快照',
  summary VARCHAR(1000) NOT NULL COMMENT '关注判断摘要',
  advice VARCHAR(2000) NOT NULL COMMENT '维修建议',
  remark VARCHAR(500) NOT NULL COMMENT '确认或关闭备注',
  handled_by VARCHAR(50) NOT NULL COMMENT '确认人',
  created_at DATETIME(6) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_advisory_run (run_id),
  KEY ix_advisory_owner (owner, created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='AI 告警研判与维修建议';

-- 2. 通知事务发件箱：与研判在同一个 SaveChanges 中提交。
-- advisory_id 唯一索引保证每条研判至多一条待办。
CREATE TABLE IF NOT EXISTS ai_notification_outbox (
  id CHAR(32) NOT NULL COMMENT '32 位无连字符 GUID',
  advisory_id CHAR(32) NOT NULL COMMENT '来源研判 ID',
  state VARCHAR(30) NOT NULL COMMENT 'Pending/Sending/Retry/Accepted/Failed/Unknown',
  attempts INT NOT NULL DEFAULT 0,
  next_at DATETIME(6) NOT NULL COMMENT '下次投递时间，UTC',
  last_error VARCHAR(200) NOT NULL,
  updated_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_notification_advisory (advisory_id),
  KEY ix_notification_ready (state, next_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='AI 告警通知待办（Outbox）';
