-- Day46：设备知识库资料、切片与引用回答
-- 在已有 iot_gateway 数据库执行。
USE iot_gateway;

-- 1. 资料文档：同一所有者 + 同一文件 + 同一版本只允许一条。
CREATE TABLE IF NOT EXISTS ai_knowledge_documents (
  id BIGINT NOT NULL AUTO_INCREMENT COMMENT '文档主键',
  owner VARCHAR(100) NOT NULL COMMENT '上传人，来自 JWT',
  device_id BIGINT NOT NULL COMMENT '所属设备 ID',
  model_code VARCHAR(80) NOT NULL COMMENT '设备型号，检索时按型号过滤',
  file_id BIGINT NOT NULL COMMENT '来源文件编号',
  revision VARCHAR(40) NOT NULL COMMENT '资料版本',
  state VARCHAR(30) NOT NULL COMMENT 'Parsing/Indexing/Ready/NeedsOcr/Failed/Retired',
  error VARCHAR(300) NULL COMMENT '失败原因，可公开',
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_kb_version (owner, file_id, revision),
  KEY ix_kb_document_model (owner, model_code, state)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='设备知识库资料';

-- 2. 资料切片：每条切片保留顺序号，引用时按 chunkId + ordinal 回指原文。
CREATE TABLE IF NOT EXISTS ai_knowledge_chunks (
  id VARCHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL COMMENT '切片 GUID',
  document_id BIGINT NOT NULL COMMENT '所属文档',
  ordinal INT NOT NULL COMMENT '块序号，从 1 开始',
  text LONGTEXT NOT NULL COMMENT '切片正文',
  PRIMARY KEY (id),
  KEY ix_kb_chunks (document_id, ordinal)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='设备知识库切片';

-- 3. 历史回答：引用快照在调用模型前固定，模型正文里的编号不作为权威引用。
CREATE TABLE IF NOT EXISTS ai_knowledge_answers (
  id BIGINT NOT NULL AUTO_INCREMENT COMMENT '回答主键',
  owner VARCHAR(100) NOT NULL COMMENT '提问人，来自 JWT',
  model_code VARCHAR(80) NOT NULL,
  question VARCHAR(1000) NOT NULL,
  answer MEDIUMTEXT NOT NULL,
  state VARCHAR(30) NOT NULL COMMENT 'NoEvidence/Running/Succeeded/Failed',
  citations_json JSON NOT NULL COMMENT '后端实际检索到的引用快照',
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  KEY ix_kb_answer_owner (owner, id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='设备知识库回答与引用';
