# Contexto do Projeto

- Aplicação Todo List para aprendizado de Claude Code e orquestração de IA
- Dev: Fullstack Pleno, 5 anos, stack principal .NET/Angular/PostgreSQL

# Stack

- Backend: .NET 10, C#, Clean Architecture, CQRS com MediatR, EF Core
- Frontend: Angular 19, Standalone Components, Signals
- Banco: PostgreSQL
- Infra: Docker Compose

# Padrões obrigatórios

- Sempre seguir Clean Architecture (dependências fluem para dentro)
- Commands para escrita, Queries para leitura (CQRS)
- Nunca colocar lógica de negócio nos Controllers
- Nunca colocar lógica de negócio nos Components Angular
- Português para comentários e commits
- Inglês para código (nomes de classes, métodos, variáveis)

# O que NUNCA fazer

- Não criar NgModules no Angular (usar Standalone)
- Não referenciar Infrastructure a partir do Domain
- Não expor entidades de domínio diretamente nas APIs (usar DTOs)

# Estrutura de pastas

TodoList/
├── backend/
│ ├── src/
│ │ ├── TodoList.Domain/ # Camada de Domínio
│ │ │ ├── Entities/
│ │ │ │ └── TodoItem.cs
│ │ │ ├── Enums/
│ │ │ │ └── TodoStatus.cs
│ │ │ ├── Interfaces/
│ │ │ │ └── Repositories/
│ │ │ │ └── ITodoRepository.cs
│ │ │ └── ValueObjects/
│ │ │ └── Title.cs
│ │ │
│ │ ├── TodoList.Application/ # Camada de Aplicação
│ │ │ ├── DTOs/
│ │ │ │ ├── TodoItemDto.cs
│ │ │ │ └── CreateTodoDto.cs
│ │ │ ├── UseCases/
│ │ │ │ ├── CreateTodo/
│ │ │ │ │ ├── CreateTodoCommand.cs
│ │ │ │ │ └── CreateTodoHandler.cs
│ │ │ │ ├── GetTodos/
│ │ │ │ │ ├── GetTodosQuery.cs
│ │ │ │ │ └── GetTodosHandler.cs
│ │ │ │ ├── UpdateTodo/
│ │ │ │ └── DeleteTodo/
│ │ │ ├── Interfaces/
│ │ │ │ └── IUnitOfWork.cs
│ │ │ └── Mappings/
│ │ │ └── TodoProfile.cs
│ │ │
│ │ ├── TodoList.Infrastructure/ # Camada de Infraestrutura
│ │ │ ├── Persistence/
│ │ │ │ ├── AppDbContext.cs
│ │ │ │ ├── Configurations/
│ │ │ │ │ └── TodoItemConfiguration.cs
│ │ │ │ ├── Migrations/
│ │ │ │ └── Repositories/
│ │ │ │ └── TodoRepository.cs
│ │ │ └── DependencyInjection.cs
│ │ │
│ │ └── TodoList.API/ # Camada de Apresentação
│ │ ├── Controllers/
│ │ │ └── TodosController.cs
│ │ ├── Middleware/
│ │ │ └── ExceptionHandlingMiddleware.cs
│ │ ├── Program.cs
│ │ └── appsettings.json
│ │
│ └── tests/
│ ├── TodoList.Domain.Tests/
│ ├── TodoList.Application.Tests/
│ └── TodoList.API.IntegrationTests/
│
├── frontend/ # Angular 19
│ ├── src/
│ │ ├── app/
│ │ │ ├── core/ # Singleton services, guards, interceptors
│ │ │ │ ├── services/
│ │ │ │ │ └── todo.service.ts
│ │ │ │ ├── interceptors/
│ │ │ │ │ └── http-error.interceptor.ts
│ │ │ │ └── models/
│ │ │ │ └── todo.model.ts
│ │ │ ├── features/ # Feature modules (standalone components)
│ │ │ │ └── todos/
│ │ │ │ ├── todo-list/
│ │ │ │ │ ├── todo-list.component.ts
│ │ │ │ │ └── todo-list.component.html
│ │ │ │ ├── todo-item/
│ │ │ │ └── todo-form/
│ │ │ ├── shared/ # Componentes e pipes reutilizáveis
│ │ │ │ ├── components/
│ │ │ │ └── pipes/
│ │ │ ├── app.component.ts
│ │ │ ├── app.config.ts
│ │ │ └── app.routes.ts
│ │ ├── environments/
│ │ └── styles.scss
│ └── angular.json
│
├── docker-compose.yml # PostgreSQL + API + Frontend
└── README.md

# Como trabalhar neste projeto

- Sempre pergunte antes de criar arquivos em massa
- Mostre o plano antes de executar
- Prefira mudanças pequenas e incrementais
- Ao encontrar um problema, apresente 2-3 opções antes de escolher
