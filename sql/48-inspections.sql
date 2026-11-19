-- Day48：设备巡检计划与巡检任务队列
-- 在已有 iot_gateway 数据库执行；设备表、点位表、采样表不重建。
USE iot_gateway;

-- 1. 巡检计划表：保存执行配置，任务创建时复制配置。
CREATE TABLE IF NOT EXISTS ai_inspection_plans (
  id BIGINT NOT NULL AUTO_INCREMENT COMMENT '计划主键',
  owner VARCHAR(50) NOT NULL COMMENT '计划所有者，来自 JWT',
  device_code VARCHAR(50) NOT NULL COMMENT '设备编码',
  point_code VARCHAR(80) NOT NULL COMMENT '项目点位编码上限为 80',
  question VARCHAR(1000) NOT NULL COMMENT '巡检问题',
  interval_seconds INT NOT NULL COMMENT '巡检周期，60-86400 秒',
  enabled TINYINT(1) NOT NULL DEFAULT 1 COMMENT '1 启用 / 0 停用',
  next_at DATETIME(6) NOT NULL COMMENT '下次调度时间，UTC',
  PRIMARY KEY (id),
  KEY ix_inspection_plan_due (enabled, next_at),
  KEY ix_inspection_plan_owner (owner, id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='AI 设备巡检计划';

-- 2. 巡检任务表：一条记录 = 一个窗口内的一次巡检任务。
-- merge_key 唯一索引负责窗口合并与并发裁决。
CREATE TABLE IF NOT EXISTS ai_inspection_runs (
  id CHAR(32) NOT NULL COMMENT '32 位无连字符 GUID',
  plan_id BIGINT NOT NULL COMMENT '来源计划',
  owner VARCHAR(50) NOT NULL COMMENT '执行身份，取自计划所有者',
  device_code VARCHAR(50) NOT NULL,
  point_code VARCHAR(80) NOT NULL,
  question VARCHAR(1000) NOT NULL,
  merge_key VARCHAR(120) NOT NULL COMMENT '计划 ID + 窗口桶，唯一',
  source VARCHAR(20) NOT NULL COMMENT 'Manual / Scheduled / Event',
  state VARCHAR(30) NOT NULL COMMENT 'Queued/Running/Retry/Succeeded/Failed/Canceled/Unknown',
  attempts INT NOT NULL DEFAULT 0 COMMENT '已尝试次数',
  evidence_json LONGTEXT NOT NULL COMMENT '首次执行冻结的证据快照',
  answer LONGTEXT NOT NULL COMMENT '模型最终回答',
  error VARCHAR(200) NOT NULL COMMENT '可公开的错误提示',
  created_at DATETIME(6) NOT NULL,
  next_at DATETIME(6) NOT NULL COMMENT '下次可执行时间，UTC',
  lease_until DATETIME(6) NULL COMMENT '执行租约，过期置 Unknown',
  completed_at DATETIME(6) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_inspection_merge (merge_key),
  KEY ix_inspection_ready (state, next_at),
  KEY ix_inspection_owner (owner, created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='AI 设备巡检任务队列';

-- 3. 设备事件接入权限：只发给受认证的桥接程序，与计划管理权限分离。
INSERT IGNORE INTO permissions (code, name, description, status)
VALUES ('ai:event:ingest', '接入设备事件', '允许桥接程序提交设备事件并触发巡检', 1);
