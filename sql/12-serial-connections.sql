-- ====================================================================
-- Day12 · 串口通信：设备串口连接参数
-- 文件：day10WpfProject/sql/12-serial-connections.sql
-- 作用：给项目数据库添加 serial_connections 表，并插入本章的虚拟串口温度仪表
-- 说明：本脚本可以重复执行（幂等）：
--       建表用 IF NOT EXISTS，插数据用 ON DUPLICATE KEY UPDATE
-- 执行环境：MySQL 8.x（数据库 iot_gateway）
-- ====================================================================

-- 使用项目已有数据库。
USE iot_gateway;

-- ====================================================================
-- 1. 建表：串口连接参数
--    为什么单独一张表，而不是往 devices 表加列：
--      ① 不是所有设备都有串口（网口/MQTT/模拟器都没有 COM 口）
--      ② 串口参数是一组"连接合同"，必须一起对才有意义
--      ③ 一台设备最多一份，用 device_id 唯一索引正好表达这个一对一关系
-- ====================================================================
CREATE TABLE IF NOT EXISTS serial_connections (
    -- 数据库内部自增主键。
    id BIGINT NOT NULL AUTO_INCREMENT COMMENT '串口连接主键',

    -- 这份串口参数属于哪台设备。
    device_id BIGINT NOT NULL COMMENT '设备主键',

    -- Windows 例如 COM11，Linux 例如 /dev/ttyUSB0。
    port_name VARCHAR(100) NOT NULL COMMENT '串口名称',

    -- 通信速率，双方必须一致。
    baud_rate INT NOT NULL DEFAULT 9600 COMMENT '波特率',

    -- 单个字符帧的数据位，常用 8。
    data_bits TINYINT NOT NULL DEFAULT 8 COMMENT '数据位',

    -- None、Odd 或 Even。
    parity VARCHAR(10) NOT NULL DEFAULT 'None' COMMENT '校验位',

    -- One、OnePointFive 或 Two。
    stop_bits VARCHAR(20) NOT NULL DEFAULT 'One' COMMENT '停止位',

    -- None、RequestToSend、XOnXOff 或 RequestToSendXOnXOff。
    handshake VARCHAR(30) NOT NULL DEFAULT 'None' COMMENT '流控',

    -- ascii 或 utf-8；二进制协议仍保存原始 byte 数组。
    text_encoding VARCHAR(20) NOT NULL DEFAULT 'ascii' COMMENT '文本编码',

    -- None、CR、LF 或 CRLF，用于文本协议分帧。
    frame_delimiter VARCHAR(10) NOT NULL DEFAULT 'CRLF' COMMENT '文本帧结束符',

    -- 一次打开串口并等待完整响应的最长时间。
    read_timeout_ms INT NOT NULL DEFAULT 2000 COMMENT '读取超时毫秒数',

    -- 串口写入阻塞的最长时间。
    write_timeout_ms INT NOT NULL DEFAULT 2000 COMMENT '写入超时毫秒数',

    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',

    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        ON UPDATE CURRENT_TIMESTAMP COMMENT '修改时间',

    PRIMARY KEY (id),

    -- 一台设备只有一份当前串口连接参数。
    UNIQUE KEY uk_serial_connections_device (device_id),

    -- 查询同一 COM 口被哪些设备配置使用。
    KEY idx_serial_connections_port_name (port_name),

    -- 连接参数不能指向不存在的设备。
    CONSTRAINT fk_serial_connections_device
        FOREIGN KEY (device_id) REFERENCES devices (id)
        ON DELETE CASCADE,

    -- 限制常见数据位。
    CONSTRAINT ck_serial_connections_data_bits
        CHECK (data_bits BETWEEN 5 AND 8),

    -- 限制校验位拼写。
    CONSTRAINT ck_serial_connections_parity
        CHECK (parity IN ('None', 'Odd', 'Even')),

    -- 限制停止位拼写。
    CONSTRAINT ck_serial_connections_stop_bits
        CHECK (stop_bits IN ('One', 'OnePointFive', 'Two')),

    -- 超时不能为 0 或负数。
    CONSTRAINT ck_serial_connections_timeouts
        CHECK (read_timeout_ms >= 100 AND write_timeout_ms >= 100)
) ENGINE = InnoDB
  DEFAULT CHARACTER SET = utf8mb4
  COMMENT = '设备串口连接参数';

-- ====================================================================
-- 2. 插入本章的"虚拟串口温度仪表"设备台账
--    注意：is_deleted / connection_status / run_status 显式写出来，
--    因为有些库这几列没有 DEFAULT 值，省略会报
--    "Field 'xxx' doesn't have a default value"
-- ====================================================================
INSERT INTO devices (
    device_code,
    name,
    device_type,
    protocol,
    ip_address,
    port,
    location,
    status,
    is_deleted,
    connection_status,
    run_status)
VALUES (
    'TEMP-SERIAL-001',
    '虚拟串口温度仪表',
    'TemperatureMeter',
    'serial-ascii',
    NULL,
    NULL,
    '开发电脑',
    1, 0, 0, 0)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    protocol = VALUES(protocol),
    is_deleted = 0;

-- ====================================================================
-- 3. 根据稳定设备编号取得数据库主键
--    不能写死 device_id = 1：主键是自增的，换台电脑就不是 1 了
-- ====================================================================
SET @serial_device_id = (
    SELECT id
    FROM devices
    WHERE device_code = 'TEMP-SERIAL-001'
    LIMIT 1
);

-- ====================================================================
-- 4. 插入这台设备的串口参数
--    WebAPI 稍后打开 COM11；模拟仪表打开配对端 COM12
-- ====================================================================
INSERT INTO serial_connections (
    device_id,
    port_name,
    baud_rate,
    data_bits,
    parity,
    stop_bits,
    handshake,
    text_encoding,
    frame_delimiter,
    read_timeout_ms,
    write_timeout_ms)
VALUES (
    @serial_device_id,
    'COM11',
    9600,
    8,
    'None',
    'One',
    'None',
    'ascii',
    'CRLF',
    2000,
    2000)
ON DUPLICATE KEY UPDATE
    port_name = VALUES(port_name),
    baud_rate = VALUES(baud_rate),
    data_bits = VALUES(data_bits),
    parity = VALUES(parity),
    stop_bits = VALUES(stop_bits),
    handshake = VALUES(handshake),
    text_encoding = VALUES(text_encoding),
    frame_delimiter = VALUES(frame_delimiter),
    read_timeout_ms = VALUES(read_timeout_ms),
    write_timeout_ms = VALUES(write_timeout_ms);

-- ====================================================================
-- 5. 校验（执行完看结果）
--    预期：正好 1 行，端口 COM11 / 9600 / 8 / None / One / None / ascii / CRLF
-- ====================================================================
SELECT
    d.device_code,
    d.name,
    d.protocol,
    s.port_name,
    s.baud_rate,
    s.data_bits,
    s.parity,
    s.stop_bits,
    s.handshake,
    s.text_encoding,
    s.frame_delimiter
FROM devices AS d
INNER JOIN serial_connections AS s
    ON s.device_id = d.id
WHERE d.device_code = 'TEMP-SERIAL-001';
