# Personality Assessment System

A comprehensive personality assessment platform built with ASP.NET Core 8.0 and modern web technologies. This system provides detailed personality analysis based on the Big Five personality traits and Myers-Briggs Type Indicator (MBTI), featuring character comparisons, compatibility matching, and administrative controls.

## 🚀 Features

### Core Functionality
- **Personality Assessment**: Multi-dimensional personality evaluation using Big Five traits
- **MBTI Analysis**: Complete Myers-Briggs Type Indicator assessment
- **Character Comparison**: Compare your personality with famous fictional characters
- **Compatibility Matching**: Find compatible users based on personality traits
- **Progress Tracking**: Visual dashboard showing assessment history and progress
- **Admin Dashboard**: Comprehensive administrative controls and analytics

### Technical Highlights
- **Real-time Updates**: Dynamic UI updates without page refreshes
- **Responsive Design**: Mobile-first, fully responsive interface
- **Secure Authentication**: Role-based access control with admin privileges
- **Performance Optimized**: Efficient database queries and caching strategies
- **Comprehensive Documentation**: Extensive technical documentation and guides

## 🛠️ Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server with Entity Framework Core 9.0
- **Authentication**: Custom JWT-based authentication system
- **API**: RESTful API with Swagger documentation

### Frontend
- **Languages**: HTML5, CSS3, JavaScript (ES6+)
- **Styling**: Modern CSS with Flexbox/Grid layouts
- **UI/UX**: Responsive design with smooth animations
- **AJAX**: Fetch API for seamless server communication

### Database
- **Primary**: SQL Server LocalDB (development)
- **ORM**: Entity Framework Core with Code-First migrations
- **Performance**: Optimized indexes and query patterns

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (included with Visual Studio)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/mohamedsameh20/Personality-Assessment.git
cd Personality-Assessment
```

### 2. Navigate to Project Directory
```bash
cd PersonalityAssessment.Api/PersonalityAssessment.Api
```

### 3. Restore Dependencies
```bash
dotnet restore
```

### 4. Setup Database
```bash
# Apply database migrations
dotnet ef database update

# Or run the database setup script
# See db-setup-guide.md for detailed instructions
```

### 5. Run the Application
```bash
dotnet run
```

The application will be available at:
- **API**: `https://localhost:5001` or `http://localhost:5000`
- **Web Interface**: `http://localhost:5000/index.html`
- **Swagger Documentation**: `http://localhost:5000/swagger`

## 📖 Documentation

### Quick Reference Guides
- [`db-setup-guide.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/db-setup-guide.md) - Database setup and configuration
- [`SSMS-QuickStart.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/SSMS-QuickStart.md) - SQL Server Management Studio guide
- [`SCORING-SYSTEM-DOCUMENTATION.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/SCORING-SYSTEM-DOCUMENTATION.md) - Personality scoring algorithms
- [`BUG_FIX_TESTING_GUIDE.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/BUG_FIX_TESTING_GUIDE.md) - Testing procedures and bug fixes

### Technical Documentation
- [`SCALABILITY-ROADMAP.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/SCALABILITY-ROADMAP.md) - System scalability planning
- [`DATABASE-PERFORMANCE-OPTIMIZATION-PLAN.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/DATABASE-PERFORMANCE-OPTIMIZATION-PLAN.md) - Performance optimization strategies
- [`CACHING-STRATEGY.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/CACHING-STRATEGY.md) - Caching implementation guide
- [`SRE-MONITORING-LOGGING-DESIGN.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/SRE-MONITORING-LOGGING-DESIGN.md) - Monitoring and logging setup

### Admin & Advanced Features
- [`ADMIN-DASHBOARD-README.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/ADMIN-DASHBOARD-README.md) - Admin dashboard usage guide
- [`SUPER-USER-CREDENTIALS.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/SUPER-USER-CREDENTIALS.md) - Super admin setup
- [`ASYNC-PROCESSING-EXAMPLES.md`](PersonalityAssessment.Api/PersonalityAssessment.Api/ASYNC-PROCESSING-EXAMPLES.md) - Asynchronous processing patterns

## 🏗️ Project Structure

```
PersonalityAssessment.Api/
├── PersonalityAssessment.Api/
│   ├── Controllers/          # API Controllers
│   │   ├── AssessmentController.cs
│   │   ├── AuthController.cs
│   │   ├── UsersController.cs
│   │   ├── CompatibilityController.cs
│   │   └── CharactersController.cs
│   ├── Models/              # Data Models
│   │   ├── PersonalityModels.cs
│   │   └── AuthModels.cs
│   ├── Services/            # Business Logic
│   │   ├── AssessmentService.cs
│   │   ├── PersonalityScorer.cs
│   │   ├── CompatibilityService.cs
│   │   └── UserService.cs
│   ├── Data/               # Database Context
│   │   └── ApplicationDbContext.cs
│   ├── Migrations/         # EF Core Migrations
│   ├── wwwroot/           # Static Web Files
│   │   ├── index.html     # Landing page
│   │   ├── dashboard.html # User dashboard
│   │   ├── characters.html # Character comparison
│   │   └── admin-dashboard.html # Admin interface
│   └── Program.cs         # Application entry point
├── Tests/                 # Unit & Integration Tests
└── Documentation/         # Comprehensive docs
```

## 🔧 Configuration

### Database Connection
Update `appsettings.json` with your database connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PersonalityAssessmentDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Set to `Development`, `Staging`, or `Production`
- `ASPNETCORE_URLS`: Configure listening URLs

## 🧪 Testing

### Manual Testing
1. Run the application: `dotnet run`
2. Navigate to `http://localhost:5000`
3. Create a user account and complete an assessment
4. Test character comparison and compatibility features
5. Access admin dashboard with admin credentials

### Automated Testing
```bash
# Run unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🚀 Deployment

### Local Development
```bash
dotnet run --environment Development
```

### Production Build
```bash
dotnet publish -c Release -o ./publish
```

### Docker (Optional)
```dockerfile
# See Dockerfile in root directory
docker build -t personality-assessment .
docker run -p 5000:80 personality-assessment
```

## 🔐 Security Features

- **Input Validation**: Comprehensive server-side validation
- **SQL Injection Protection**: Parameterized queries via Entity Framework
- **XSS Prevention**: Output encoding and Content Security Policy
- **Authentication**: Secure user authentication system
- **Authorization**: Role-based access control
- **HTTPS**: SSL/TLS encryption support

## 🎯 API Endpoints

### Assessment
- `POST /api/assessment/submit` - Submit assessment responses
- `GET /api/assessment/questions` - Retrieve assessment questions

### User Management
- `GET /api/users/{id}/profile` - Get user personality profile
- `GET /api/users/{id}/stats` - Get user statistics
- `POST /api/auth/login` - User authentication
- `POST /api/auth/register` - User registration

### Compatibility & Characters
- `GET /api/compatibility/matches/{userId}` - Find compatible matches
- `GET /api/characters` - List available characters
- `POST /api/characters/{id}/compare` - Compare with character

### Admin (Requires Admin Role)
- `GET /api/admin/users` - List all users
- `GET /api/admin/stats` - System statistics
- `DELETE /api/admin/users/{id}` - Delete user

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m "Add some AmazingFeature"`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Development Guidelines
- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Test across different browsers
- Ensure responsive design compatibility

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Mohamed Sameh** - *Initial work and development* - [@mohamedsameh20](https://github.com/mohamedsameh20)

## 🙏 Acknowledgments

- Big Five personality model research
- Myers-Briggs Type Indicator framework
- ASP.NET Core community
- Entity Framework Core team
- Open source contributors

## 📞 Support

For support, questions, or feature requests:
- Open an issue on GitHub
- Check existing documentation in the `/docs` folder
- Review the troubleshooting guides

---

**Built with ❤️ using ASP.NET Core 8.0**
