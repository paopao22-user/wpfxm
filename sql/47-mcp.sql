-- ============================================================
-- Day47 MCP 设备工具接入与权限管理
-- 在 DataGrip 执行；VS 的 T-SQL 校验会把 MySQL 语法误报成红线。
-- ============================================================

-- 一条授权 = 某个账号可以调用某台设备的某个工具。
-- 不预置任何"全权限"授权，必须显式授予。
CREATE TABLE IF NOT EXISTS ai_mcp_grants (
    actor       VARCHAR(50) COLLATE utf8mb4_bin NOT NULL,
    device_code VARCHAR(50) COLLATE utf8mb4_bin NOT NULL,
    tool        VARCHAR(50) COLLATE utf8mb4_bin NOT NULL,
    granted_by  VARCHAR(50) NOT NULL,
    granted_at  DATETIME(6) NOT NULL,
    PRIMARY KEY (actor, device_code, tool)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- 每次工具调用一行审计。
-- Started 只代表"已进入业务执行边界"，只有拿到真实返回才改成 Succeeded。
CREATE TABLE IF NOT EXISTS ai_mcp_calls (
    id             CHAR(32) NOT NULL,
    actor          VARCHAR(50) NOT NULL,
    device_code    VARCHAR(50) NOT NULL,
    tool           VARCHAR(50) NOT NULL,
    arguments_json JSON NOT NULL,
    status         VARCHAR(30) NOT NULL,
    result_json    LONGTEXT NOT NULL,
    started_at     DATETIME(6) NOT NULL,
    completed_at   DATETIME(6) NULL,
    PRIMARY KEY (id),
    INDEX ix_mcp_actor_time (actor, started_at)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- 授权变更单独留痕：撤销授权只能删 ai_mcp_grants，
-- 不能把"谁在什么时候授予/撤销过"这段历史一起抹掉。
CREATE TABLE IF NOT EXISTS ai_mcp_grant_changes (
    id          BIGINT NOT NULL AUTO_INCREMENT,
    operator    VARCHAR(50) NOT NULL,
    actor       VARCHAR(50) NOT NULL,
    device_code VARCHAR(50) NOT NULL,
    tool        VARCHAR(50) NOT NULL,
    action      VARCHAR(20) NOT NULL,
    occurred_at DATETIME(6) NOT NULL,
    PRIMARY KEY (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;
