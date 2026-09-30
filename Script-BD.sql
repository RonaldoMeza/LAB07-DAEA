-- Base de datos de la solución Biblioteca (Semana 07: arquitectura en capas)
IF DB_ID('BibliotecaDB') IS NOT NULL
BEGIN
	ALTER DATABASE BibliotecaDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE BibliotecaDB;
END
GO
CREATE DATABASE BibliotecaDB;
GO
USE BibliotecaDB;
GO

-- Autores de los libros. Activo permite la eliminación lógica.
CREATE TABLE Autores (
	AutorId      INT IDENTITY(1,1) PRIMARY KEY,
	Nombre       NVARCHAR(100) NOT NULL,
	Nacionalidad NVARCHAR(50)  NULL,
	Activo       BIT           NOT NULL DEFAULT 1
);
GO

-- Libros. ISBN único; Ejemplares es el stock disponible para préstamo.
CREATE TABLE Libros (
	LibroId    INT IDENTITY(1,1) PRIMARY KEY,
	Titulo     NVARCHAR(150) NOT NULL,
	ISBN       NVARCHAR(20)  NOT NULL UNIQUE,
	AutorId    INT           NOT NULL REFERENCES Autores(AutorId),
	Ejemplares INT           NOT NULL DEFAULT 0,
	Activo     BIT           NOT NULL DEFAULT 1
);
GO

-- Socios de la biblioteca. DNI único.
CREATE TABLE Socios (
	SocioId INT IDENTITY(1,1) PRIMARY KEY,
	DNI     NVARCHAR(15)  NOT NULL UNIQUE,
	Nombre  NVARCHAR(100) NOT NULL,
	Email   NVARCHAR(100) NULL,
	Activo  BIT           NOT NULL DEFAULT 1
);
GO

-- Cabecera del préstamo. Estado: Prestado / Devuelto.
CREATE TABLE Prestamos (
	PrestamoId    INT IDENTITY(1,1) PRIMARY KEY,
	SocioId       INT      NOT NULL REFERENCES Socios(SocioId),
	FechaPrestamo DATETIME NOT NULL DEFAULT GETDATE(),
	FechaLimite   DATETIME NOT NULL,
	Estado        NVARCHAR(20) NOT NULL DEFAULT 'Prestado'
);
GO

-- Detalle: qué libros lleva cada préstamo. Clave primaria compuesta.
-- FechaDevolucion queda en NULL mientras el libro siga prestado.
CREATE TABLE DetallePrestamo (
	PrestamoId      INT      NOT NULL REFERENCES Prestamos(PrestamoId),
	LibroId         INT      NOT NULL REFERENCES Libros(LibroId),
	FechaDevolucion DATETIME NULL,
	CONSTRAINT PK_DetallePrestamo PRIMARY KEY (PrestamoId, LibroId)
);
GO

-- Datos de prueba: 8 autores
INSERT INTO Autores (Nombre, Nacionalidad) VALUES
	(N'Gabriel García Márquez', N'Colombiana'),
	(N'Mario Vargas Llosa',     N'Peruana'),
	(N'Jorge Luis Borges',      N'Argentina'),
	(N'Isabel Allende',         N'Chilena'),
	(N'Julio Cortázar',         N'Argentina'),
	(N'Pablo Neruda',           N'Chilena'),
	(N'Miguel de Cervantes',    N'Española'),
	(N'Jane Austen',            N'Británica');
GO

-- Datos de prueba: 20 libros
INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares) VALUES
	(N'Cien años de soledad',              N'9780307474728', 1, 5),
	(N'El amor en los tiempos del cólera', N'9780307389732', 1, 3),
	(N'Crónica de una muerte anunciada',   N'9780307475770', 1, 2),
	(N'La ciudad y los perros',            N'9788420412146', 2, 4),
	(N'La casa verde',                     N'9788420420622', 2, 2),
	(N'Conversación en La Catedral',       N'9788420419183', 2, 3),
	(N'Ficciones',                         N'9780307950925', 3, 4),
	(N'El Aleph',                          N'9780307950932', 3, 3),
	(N'La casa de los espíritus',          N'9781501117015', 4, 5),
	(N'De amor y de sombra',               N'9781501108624', 4, 2),
	(N'Rayuela',                           N'9788437604747', 5, 3),
	(N'Bestiario',                         N'9788437601883', 5, 2),
	(N'Veinte poemas de amor',             N'9788437604242', 6, 6),
	(N'Residencia en la tierra',           N'9788437604259', 6, 2),
	(N'Don Quijote de la Mancha',          N'9788420412147', 7, 4),
	(N'Novelas ejemplares',                N'9788437604273', 7, 3),
	(N'Orgullo y prejuicio',               N'9788497940074', 8, 4),
	(N'Emma',                              N'9788497940081', 8, 2),
	(N'Sentido y sensibilidad',            N'9788497940098', 8, 3),
	(N'Persuasión',                        N'9788497940104', 8, 0);
GO

-- Datos de prueba: 10 socios
INSERT INTO Socios (DNI, Nombre, Email) VALUES
	(N'45678901', N'Ana Torres Ríos',    N'ana.torres@mail.com'),
	(N'45678902', N'Luis Ramos Paredes', N'luis.ramos@mail.com'),
	(N'45678903', N'Carla Vega Soto',    N'carla.vega@mail.com'),
	(N'45678904', N'Diego Flores Luna',  N'diego.flores@mail.com'),
	(N'45678905', N'María Paredes Gil',  N'maria.paredes@mail.com'),
	(N'45678906', N'Jorge Campos Ruiz',  N'jorge.campos@mail.com'),
	(N'45678907', N'Lucía Mendoza Paz',  N'lucia.mendoza@mail.com'),
	(N'45678908', N'Pedro Salazar Ríos', N'pedro.salazar@mail.com'),
	(N'45678909', N'Rosa Quispe Huamán', N'rosa.quispe@mail.com'),
	(N'45678910', N'Andrés León Torres', N'andres.leon@mail.com');
GO

-- Datos de prueba: 5 préstamos con su detalle.
-- El préstamo 2 (socio 2) tiene 3 libros pendientes: sirve para probar la regla
-- de "un socio no puede tener más de 3 libros pendientes".
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
	(1, DATEADD(DAY, -20, GETDATE()), DATEADD(DAY, -6,  GETDATE()), 'Prestado'),  -- 1: vencido, pendiente
	(2, DATEADD(DAY, -10, GETDATE()), DATEADD(DAY,  4,  GETDATE()), 'Prestado'),  -- 2: 3 libros pendientes
	(3, DATEADD(DAY, -30, GETDATE()), DATEADD(DAY, -16, GETDATE()), 'Devuelto'),  -- 3: devuelto completo
	(4, DATEADD(DAY,  -5, GETDATE()), DATEADD(DAY,  9,  GETDATE()), 'Prestado'),  -- 4: al día
	(5, DATEADD(DAY, -15, GETDATE()), DATEADD(DAY, -1,  GETDATE()), 'Prestado');  -- 5: parcialmente devuelto
GO

INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
	(1, 1,  NULL),                                                          -- pendiente y vencido
	(2, 4,  NULL),                                                          -- socio 2: pendiente 1 de 3
	(2, 7,  NULL),                                                          -- socio 2: pendiente 2 de 3
	(2, 9,  NULL),                                                          -- socio 2: pendiente 3 de 3
	(3, 13, DATEADD(DAY, -18, GETDATE())),                                  -- devuelto
	(3, 17, DATEADD(DAY, -18, GETDATE())),                                  -- devuelto
	(4, 5,  NULL),                                                          -- pendiente al día
	(5, 11, DATEADD(DAY, -3, GETDATE())),                                   -- devuelto
	(5, 15, NULL);                                                          -- pendiente y vencido
GO
