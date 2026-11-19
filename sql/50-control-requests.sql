-- ============================================================
-- Day50 控制申请人工审批与操作审计
-- 本章只做审批与审计：申请保存成功不发送命令，审批通过也不发送命令。
-- 在 DataGrip 执行；VS 的 T-SQL 校验会把 MySQL 语法误报成红线。
-- ============================================================
USE iot_gateway;

-- 1. 控制申请。状态只落 Pending / Approved / Rejected / Canceled，
--    Expired 是页面按时间算出来的显示状态，不写库。
CREATE TABLE IF NOT EXISTS control_requests (
    id             CHAR(32) NOT NULL,
    request_key    CHAR(32) NOT NULL COMMENT '客户端幂等键，32 位无横线 UUID',
    device_code    VARCHAR(50) NOT NULL,
    action         VARCHAR(40) NOT NULL COMMENT 'setTemperature / powerOff',
    target         DECIMAL(5,1) NULL COMMENT '设温目标；关机为 NULL',
    reason         VARCHAR(500) NOT NULL,
    requester      VARCHAR(50) NOT NULL COMMENT '申请人，取自 JWT',
    reviewer       VARCHAR(50) NOT NULL COMMENT '审批人，取自 JWT',
    state          VARCHAR(30) NOT NULL,
    created_at     DATETIME(6) NOT NULL,
    review_before  DATETIME(6) NOT NULL COMMENT '30 分钟内允许审批',
    execute_before DATETIME(6) NULL COMMENT '批准后 5 分钟内允许开始执行',
    updated_at     DATETIME(6) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_control_request_key (request_key),
    KEY ix_control_queue (state, created_at)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COMMENT='设备控制申请';

-- 2. 操作审计。只记录人对申请做了什么，不记录设备命令和前后值。
CREATE TABLE IF NOT EXISTS control_audits (
    id         BIGINT NOT NULL AUTO_INCREMENT,
    request_id CHAR(32) NOT NULL,
    actor      VARCHAR(50) NOT NULL,
    action     VARCHAR(40) NOT NULL COMMENT 'Submit / Approve / Reject / Cancel',
    remark     VARCHAR(500) NOT NULL,
    created_at DATETIME(6) NOT NULL,
    PRIMARY KEY (id),
    KEY ix_control_audit (request_id, id),
    CONSTRAINT fk_control_audit FOREIGN KEY (request_id) REFERENCES control_requests (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COMMENT='控制申请操作审计';

-- 3. 新增审批权限。只登记权限，具体分配给哪个角色走权限管理页面，不默认发给所有操作员。
INSERT IGNORE INTO permissions (code, name, description, status)
VALUES ('device:approve', '审批设备控制', '审批他人控制申请及核对未知结果', 1);
