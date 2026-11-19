-- Day44：AI 设备分析记录表
-- 在已有 iot_gateway 数据库执行；设备表、点位表和采样历史表不重建。
USE iot_gateway;

CREATE TABLE IF NOT EXISTS ai_analysis_records (
 id BIGINT NOT NULL AUTO_INCREMENT COMMENT '分析记录主键',
 request_id VARCHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL COMMENT '一次分析意图的UUID',
 created_by VARCHAR(100) NOT NULL COMMENT 'JWT中的用户名称',
 device_code VARCHAR(50) NOT NULL,
 point_code VARCHAR(80) NOT NULL COMMENT '项目点位编码上限为80',
 window_minutes INT NOT NULL,
 window_start DATETIME(6) NOT NULL COMMENT 'UTC窗口起点',
 window_end DATETIME(6) NOT NULL COMMENT 'UTC窗口终点，不包含',
 input_json JSON NOT NULL COMMENT '本次真实输入快照',
 prompt_version VARCHAR(30) NOT NULL,
 system_prompt TEXT NOT NULL COMMENT '实际使用的应用指令',
 model_id VARCHAR(100) NOT NULL,
 status VARCHAR(30) NOT NULL,
 model_attempted TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否进入模型请求阶段',
 answer MEDIUMTEXT NOT NULL,
 error_code VARCHAR(40) NULL,
 error_message VARCHAR(500) NULL,
 input_tokens BIGINT NULL,
 output_tokens BIGINT NULL,
 elapsed_ms BIGINT NOT NULL DEFAULT 0,
 created_at DATETIME(6) NOT NULL,
 completed_at DATETIME(6) NULL,
 PRIMARY KEY (id),
 UNIQUE KEY uk_ai_analysis_request (request_id),
 KEY ix_ai_analysis_owner_device (created_by, device_code, id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
