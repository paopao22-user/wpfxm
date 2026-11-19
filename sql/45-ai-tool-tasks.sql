-- Day45：AI 设备调查任务表 + 最小告警表
-- 在已有 iot_gateway 数据库执行；设备表、点位表和采样历史表不重建。
USE iot_gateway;

-- 1. 调查任务表。point_code 使用项目现状的 80，而不是文档示例的 50。
CREATE TABLE IF NOT EXISTS ai_tool_tasks (
  id BIGINT NOT NULL AUTO_INCREMENT COMMENT '任务主键',
  request_key VARCHAR(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL COMMENT '客户端唯一意图编号',
  actor VARCHAR(100) NOT NULL COMMENT 'JWT 中的用户名称',
  device_code VARCHAR(50) NOT NULL,
  point_code VARCHAR(80) NOT NULL COMMENT '项目点位编码上限为 80',
  question VARCHAR(1000) NOT NULL COMMENT '用户问题，只作为待回答的数据',
  status VARCHAR(30) NOT NULL COMMENT 'Running/Succeeded/Failed/Canceled',
  answer MEDIUMTEXT NOT NULL COMMENT '模型最终文字',
  calls_json JSON NOT NULL COMMENT '真实工具执行过程，不是模型推理文本',
  error_message VARCHAR(300) NULL,
  created_at DATETIME(6) NOT NULL COMMENT '任务创建 UTC 时间',
  completed_at DATETIME(6) NULL COMMENT '完成或失败 UTC 时间',
  PRIMARY KEY (id),
  UNIQUE KEY uk_tool_task_request (request_key),
  KEY ix_tool_task_actor (actor, id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 如果之前按文档建过 50 长度的表，这里安全扩宽到 80。
ALTER TABLE ai_tool_tasks MODIFY COLUMN point_code VARCHAR(80) NOT NULL;

-- 2. 最小告警表：Day45 的 GetAlarms 工具需要它。
-- 这是教学用的最小表，不是完整告警模块；Day49 会继续扩展告警研判。
CREATE TABLE IF NOT EXISTS alarms (
  id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  device_code VARCHAR(50) NOT NULL,
  title VARCHAR(200) NOT NULL,
  status_code VARCHAR(30) NOT NULL COMMENT 'Active/Acknowledged 等',
  occurred_at DATETIME(6) NOT NULL COMMENT '告警发生 UTC 时间',
  KEY ix_alarms_device_status (device_code, status_code, occurred_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. 给 PLC-001 准备两条可查询的告警；重复执行不会重复插入。
INSERT INTO alarms (device_code, title, status_code, occurred_at)
SELECT 'PLC-001', '温度超过预警阈值', 'Active', UTC_TIMESTAMP(6)
WHERE NOT EXISTS (
  SELECT 1 FROM alarms
  WHERE device_code = 'PLC-001' AND title = '温度超过预警阈值'
);

INSERT INTO alarms (device_code, title, status_code, occurred_at)
SELECT 'PLC-001', '通讯短时中断', 'Acknowledged', UTC_TIMESTAMP(6) - INTERVAL 10 MINUTE
WHERE NOT EXISTS (
  SELECT 1 FROM alarms
  WHERE device_code = 'PLC-001' AND title = '通讯短时中断'
);
