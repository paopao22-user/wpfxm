-- ====================================================================
-- 阶段七：设备台账 30 条工业真实测试数据播种脚本
-- 覆盖多样化设备类型、通信协议、车间工位、在线与运行状态，以及软删除对照项
-- ====================================================================

-- 1. PLC 与运动控制器类设备 (PLC-002 ~ PLC-008)
INSERT INTO `devices` (`device_code`, `name`, `device_type`, `protocol`, `ip_address`, `port`, `location`, `status`, `is_deleted`, `connection_status`, `run_status`) VALUES
('PLC-002', '二号冲压机 PLC (西门子 S7-1500)', 'PLC', 'modbus-tcp', '192.168.1.102', 502, '一号冲压车间 A-02', 1, 0, 1, 1),
('PLC-003', '三号注塑机 PLC (三菱 FX5U)', 'PLC', 'modbus-tcp', '192.168.1.103', 502, '二号注塑车间 B-01', 1, 0, 1, 1),
('PLC-004', '装配线主控 PLC (欧姆龙 NJ501)', 'PLC', 'modbus-tcp', '192.168.1.104', 502, '总装车间线头 C-01', 1, 0, 1, 2),
('PLC-005', '立式包装机 PLC (汇川 AM600)', 'PLC', 'modbus-tcp', '192.168.1.105', 502, '后道包装车间 D-03', 1, 0, 2, 0),
('PLC-006', '立体仓库堆垛机 PLC (西门子 S7-1200)', 'PLC', 'modbus-tcp', '192.168.1.106', 502, '智能立体库巷道 01', 1, 0, 1, 1),
('PLC-007', 'AGV 调度中控 PLC', 'PLC', 'modbus-tcp', '192.168.1.107', 502, '物流中转区调度台', 1, 0, 1, 1),
('PLC-008', '涂布烘箱温控 PLC', 'PLC', 'modbus-tcp', '192.168.1.108', 502, '涂布车间干燥段 E-02', 1, 0, 3, 3);

-- 2. 空压机与动力暖通设备 (AC-001 ~ AC-006)
INSERT INTO `devices` (`device_code`, `name`, `device_type`, `protocol`, `ip_address`, `port`, `location`, `status`, `is_deleted`, `connection_status`, `run_status`) VALUES
('AC-001', '一号螺杆空压机 (阿特拉斯 GA55)', 'AirCompressor', 'modbus-tcp', '192.168.2.10', 502, '一号空压站东侧', 1, 0, 1, 1),
('AC-002', '二号变频离心空压机 (英格索兰)', 'AirCompressor', 'modbus-tcp', '192.168.2.11', 502, '一号空压站西侧', 1, 0, 1, 1),
('AC-003', '冷冻干燥机 (汉晕冷干机)', 'Dryer', 'modbus-tcp', '192.168.2.12', 502, '一号空压站预处理间', 1, 0, 1, 1),
('AC-004', '办公区多联机中央空调', 'AirConditioner', 'mqtt', NULL, NULL, '办公大楼楼顶平台', 1, 0, 1, 1),
('AC-005', '万级无尘恒温恒湿空调机组', 'AirConditioner', 'modbus-tcp', '192.168.2.20', 502, '无尘车间专用空调机房', 1, 0, 1, 1),
('AC-006', '冷水机组 (开利螺杆冷水机)', 'Chiller', 'modbus-tcp', '192.168.2.21', 502, '制冷机房 01机位', 1, 0, 2, 0);

-- 3. 智能电力仪表与能耗计量设备 (METER-001 ~ METER-006)
INSERT INTO `devices` (`device_code`, `name`, `device_type`, `protocol`, `ip_address`, `port`, `location`, `status`, `is_deleted`, `connection_status`, `run_status`) VALUES
('METER-001', '10kV 进线总多功能电表 (安科瑞)', 'ElectricityMeter', 'modbus-tcp', '192.168.3.1', 502, '高压配电房 1#进线柜', 1, 0, 1, 1),
('METER-002', '冲压车间动力变压器出线电表', 'ElectricityMeter', 'modbus-tcp', '192.168.3.2', 502, '低压配电房 2#动力柜', 1, 0, 1, 1),
('METER-003', '注塑车间加热负荷专用电表', 'ElectricityMeter', 'modbus-tcp', '192.168.3.3', 502, '车间二级配电箱 B-05', 1, 0, 1, 1),
('METER-004', '锅炉房主管道蒸汽涡街流量计', 'FlowMeter', 'modbus-tcp', '192.168.3.10', 502, '锅炉房主蒸汽母管', 1, 0, 1, 1),
('METER-005', '纯水处理车间电磁水流量计', 'FlowMeter', 'modbus-tcp', '192.168.3.11', 502, '纯水制备站出水管路', 1, 0, 1, 1),
('METER-006', '空压站主管网压缩空气流量计', 'FlowMeter', 'modbus-tcp', '192.168.3.12', 502, '空压站储气罐总出口', 1, 0, 1, 1);

-- 4. 环境监测与高精度传感设备 (ENV-001 ~ ENV-005)
INSERT INTO `devices` (`device_code`, `name`, `device_type`, `protocol`, `ip_address`, `port`, `location`, `status`, `is_deleted`, `connection_status`, `run_status`) VALUES
('ENV-001', '核心服务器机房温湿度巡检仪', 'TempHumiditySensor', 'modbus-tcp', '192.168.4.1', 502, '数据中心机房 02 列机柜', 1, 0, 1, 1),
('ENV-002', '涂装喷漆车间 VOC 气体浓度变送器', 'GasSensor', 'modbus-tcp', '192.168.4.2', 502, '涂装防爆喷漆房排风口', 1, 0, 1, 1),
('ENV-003', '化学品危化库可燃气体报警仪', 'GasSensor', 'modbus-tcp', '192.168.4.3', 502, '原料危化品仓库东墙', 1, 0, 1, 1),
('ENV-004', '洁净室粒子计数与差压传感器', 'PressureSensor', 'modbus-tcp', '192.168.4.4', 502, '洁净室缓冲区前室', 1, 0, 1, 1),
('ENV-005', '热处理淬火多通道温度巡检记录仪', 'TempRecorder', 'modbus-tcp', '192.168.4.5', 502, '热处理网带炉 3#控温点', 1, 0, 1, 1);

-- 5. 数控机床与机器人工作站 (CNC-001 ~ CNC-003, ROBOT-001 ~ ROBOT-002)
INSERT INTO `devices` (`device_code`, `name`, `device_type`, `protocol`, `ip_address`, `port`, `location`, `status`, `is_deleted`, `connection_status`, `run_status`) VALUES
('CNC-001', '立式加工中心 (发那科 0i-MF)', 'CNC', 'opc-ua', '192.168.5.1', 4840, '精密机加车间 CNC-01', 1, 0, 1, 1),
('CNC-002', '卧式车削中心 (马扎克 QT250)', 'CNC', 'opc-ua', '192.168.5.2', 4840, '精密机加车间 CNC-02', 1, 0, 1, 1),
('CNC-003', '五轴联动龙门加工中心 (德玛吉)', 'CNC', 'opc-ua', '192.168.5.3', 4840, '重型模具加工区', 1, 0, 2, 0),
('ROBOT-001', '六轴点焊工业机器人 (发那科 M-20iD)', 'Robot', 'modbus-tcp', '192.168.5.10', 502, '白车身自动焊接工位 01', 1, 0, 1, 1),
('ROBOT-002', '码垛包装桁架机械手 (川崎 CP180L)', 'Robot', 'modbus-tcp', '192.168.5.11', 502, '成品立体码垛工位 02', 1, 0, 1, 2);

-- 6. 边缘虚拟测试网关与软删除对照设备 (SIM-001, DEL-001, DEL-002)
INSERT INTO `devices` (`device_code`, `name`, `device_type`, `protocol`, `ip_address`, `port`, `location`, `status`, `is_deleted`, `connection_status`, `run_status`) VALUES
('SIM-001', '多参数工业现场全要素模拟仿真器', 'Simulator', 'simulator', NULL, NULL, '边缘网关虚拟回环沙箱', 1, 0, 1, 1),
('DEL-001', '已报废注塑机 (历史归档备查)', 'InjectionMolding', 'modbus-tcp', '192.168.9.91', 502, '旧厂区废弃存放库', 0, 1, 2, 4),
('DEL-002', '已下线测试试验台 (软删除对照)', 'TestBench', 'modbus-tcp', '192.168.9.92', 502, '研发工程部中试车间', 0, 1, 2, 4);
