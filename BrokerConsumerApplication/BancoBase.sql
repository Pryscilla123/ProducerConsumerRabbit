CREATE DATABASE Producao;

CREATE TABLE armazem
(
	id UNIQUEIDENTIFIER PRIMARY KEY,
	nome VARCHAR(100),
	tipo VARCHAR(25)
);

CREATE TABLE enderco
(
	armazem_id UNIQUEIDENTIFIER PRIMARY KEY,
	rua VARCHAR(100),
	bairro VARCHAR(100),
	numero integer,
	cidade VARCHAR(100),
	estado VARCHAR(100),
	cep VARCHAR(9),
	CONSTRAINT fk_armazem_id FOREIGN KEY (armazem_id) REFERENCES armazem(id)
);