using HistoricalReporting.Core.Entities;
using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Core.Models;

namespace HistoricalReporting.Core.Services;

public class DataSeedService : IDataSeedService
{
    private readonly IDataSeedRepository _repository;
    private readonly Random _random = new();

    private static readonly string[] OrganisationNames =
    [
        "ACMECOR", "GLOBTEX", "SYNTHCO", "NEXAGEN", "PRIMTEC",
    ];

    private static readonly string[] DepartmentNames =
    [
        "Accounting", "Administration", "Analytics", "Architecture", "Asset Management",
        "Audit", "Benefits", "Brand Management", "Budget", "Business Development",
        "Compliance", "Construction", "Consulting", "Content", "Contracts",
        "Corporate Affairs", "Customer Service", "Data Science", "Design", "Distribution",
        "Documentation", "eCommerce", "Engineering", "Environmental", "Events",
        "Facilities", "Finance", "Fleet Management", "Governance", "Government Relations",
        "Health and Safety", "Help Desk", "Human Resources", "Information Security", "Infrastructure",
        "Innovation", "Insurance", "Internal Audit", "Investor Relations", "IT Support",
        "Knowledge Management", "Legal", "Licensing", "Logistics", "Maintenance",
        "Manufacturing", "Marketing", "Media Relations", "Merchandising", "Network Operations",
        "Operations", "Partnerships", "Payroll", "Performance Management", "Planning",
        "Procurement", "Product Development", "Product Management", "Production", "Project Management",
        "Public Relations", "Quality Assurance", "Quality Control", "Real Estate", "Receiving",
        "Records Management", "Recruitment", "Regulatory Affairs", "Research", "Revenue",
        "Risk Management", "Sales", "Security", "Service Delivery", "Shipping",
        "Social Media", "Software Development", "Strategic Planning", "Supply Chain", "Sustainability",
        "Tax", "Technical Support", "Technology", "Telecommunications", "Training",
        "Transportation", "Treasury", "User Experience", "Vendor Management", "Warehouse",
        "Web Development", "Wellness", "Workforce Planning", "Customer Success", "DevOps",
        "Cloud Services", "Mobile Development", "Platform Engineering", "Site Reliability", "Data Engineering"
    ];

    public DataSeedService(IDataSeedRepository repository)
    {
        _repository = repository;
    }

    public async Task SeedDataAsync(Func<SeedProgress, Task> progressCallback, CancellationToken cancellationToken = default)
    {
        try
        {
            const int orgCount = 5;
            const int managersPerOrg = 5;
            const int departmentsPerOrg = 50;
            const int subDepartmentsPerOrg = 35;
            const int usersPerOrg = 5000;
            const int minAuditRecords = 3;
            const int maxAuditRecords = 10;

            var totalOrgs = orgCount;
            var totalManagers = orgCount * managersPerOrg;
            var totalDepartments = orgCount * departmentsPerOrg;
            var totalUsers = orgCount * usersPerOrg;
            var estimatedAuditRecords = totalUsers * ((minAuditRecords + maxAuditRecords) / 2);

            // Stage 1: Create Organisations
            await progressCallback(new SeedProgress
            {
                Stage = "Organisations",
                Message = "Creating organisations...",
                CurrentItem = 0,
                TotalItems = totalOrgs,
                PercentComplete = 0
            });

            var organisations = new List<Organisation>();
            for (int i = 0; i < orgCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var org = new Organisation
                {
                    Id = Guid.NewGuid(),
                    Name = OrganisationNames[i]
                };
                organisations.Add(org);

                await progressCallback(new SeedProgress
                {
                    Stage = "Organisations",
                    Message = $"Created organisation: {org.Name}",
                    CurrentItem = i + 1,
                    TotalItems = totalOrgs,
                    PercentComplete = CalculateOverallProgress(1, 6, (i + 1.0) / totalOrgs)
                });
            }

            await _repository.AddOrganisationsAsync(organisations, cancellationToken);

            // Stage 2: Create Departments for each organisation
            await progressCallback(new SeedProgress
            {
                Stage = "Departments",
                Message = "Creating departments...",
                CurrentItem = 0,
                TotalItems = totalDepartments,
                PercentComplete = CalculateOverallProgress(2, 6, 0)
            });

            var allDepartments = new List<Department>();
            var departmentsByOrg = new Dictionary<Guid, List<Department>>();

            foreach (var org in organisations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var shuffledNames = DepartmentNames.OrderBy(_ => _random.Next()).Take(departmentsPerOrg).ToList();
                var orgDepartments = new List<Department>();

                for (int i = 0; i < departmentsPerOrg; i++)
                {
                    var dept = new Department
                    {
                        Id = Guid.NewGuid(),
                        Name = shuffledNames[i],
                        OrganisationId = org.Id,
                        ParentDepartmentId = null
                    };
                    orgDepartments.Add(dept);
                    allDepartments.Add(dept);
                }

                departmentsByOrg[org.Id] = orgDepartments;

                await progressCallback(new SeedProgress
                {
                    Stage = "Departments",
                    Message = $"Created {departmentsPerOrg} departments for {org.Name}",
                    CurrentItem = allDepartments.Count,
                    TotalItems = totalDepartments,
                    PercentComplete = CalculateOverallProgress(2, 6, (double)allDepartments.Count / totalDepartments)
                });
            }

            await _repository.AddDepartmentsAsync(allDepartments, cancellationToken);

            // Stage 3: Create sub-department hierarchy
            await progressCallback(new SeedProgress
            {
                Stage = "Sub-Departments",
                Message = "Creating department hierarchy...",
                CurrentItem = 0,
                TotalItems = orgCount * subDepartmentsPerOrg,
                PercentComplete = CalculateOverallProgress(3, 6, 0)
            });

            var hierarchyUpdates = new List<(Guid DeptId, Guid ParentId)>();
            int hierarchyProgress = 0;

            foreach (var org in organisations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var orgDepts = departmentsByOrg[org.Id];
                var rootDepartments = orgDepts.Take(departmentsPerOrg - subDepartmentsPerOrg).ToList();
                var subDepartments = orgDepts.Skip(departmentsPerOrg - subDepartmentsPerOrg).ToList();

                var possibleParents = new List<Department>(rootDepartments);

                foreach (var subDept in subDepartments)
                {
                    var parentIndex = _random.Next(possibleParents.Count);
                    var parent = possibleParents[parentIndex];

                    hierarchyUpdates.Add((subDept.Id, parent.Id));
                    possibleParents.Add(subDept);
                    hierarchyProgress++;
                }

                await progressCallback(new SeedProgress
                {
                    Stage = "Sub-Departments",
                    Message = $"Created hierarchy for {org.Name}",
                    CurrentItem = hierarchyProgress,
                    TotalItems = orgCount * subDepartmentsPerOrg,
                    PercentComplete = CalculateOverallProgress(3, 6, (double)hierarchyProgress / (orgCount * subDepartmentsPerOrg))
                });
            }

            await _repository.UpdateDepartmentHierarchyAsync(hierarchyUpdates, cancellationToken);

            // Stage 4: Create Managers
            await progressCallback(new SeedProgress
            {
                Stage = "Managers",
                Message = "Creating managers...",
                CurrentItem = 0,
                TotalItems = totalManagers,
                PercentComplete = CalculateOverallProgress(4, 6, 0)
            });

            var allManagers = new List<Manager>();
            var managerDepartments = new List<ManagerDepartment>();
            int managerProgress = 0;

            foreach (var org in organisations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var orgDepts = departmentsByOrg[org.Id];
                var orgManagers = new List<Manager>();

                for (int i = 0; i < managersPerOrg; i++)
                {
                    var manager = new Manager
                    {
                        Id = Guid.NewGuid(),
                        OrganisationId = org.Id,
                        ManagesAllDepartments = i == 0
                    };
                    orgManagers.Add(manager);
                    allManagers.Add(manager);
                    managerProgress++;
                }

                var nonGlobalManagers = orgManagers.Skip(1).ToList();
                var shuffledDepts = orgDepts.OrderBy(_ => _random.Next()).ToList();
                var deptsPerManager = shuffledDepts.Count / nonGlobalManagers.Count;

                for (int i = 0; i < nonGlobalManagers.Count; i++)
                {
                    var manager = nonGlobalManagers[i];
                    var assignedDepts = shuffledDepts
                        .Skip(i * deptsPerManager)
                        .Take(i == nonGlobalManagers.Count - 1 ? int.MaxValue : deptsPerManager);

                    foreach (var dept in assignedDepts)
                    {
                        managerDepartments.Add(new ManagerDepartment
                        {
                            ManagerId = manager.Id,
                            DepartmentId = dept.Id
                        });
                    }
                }

                await progressCallback(new SeedProgress
                {
                    Stage = "Managers",
                    Message = $"Created {managersPerOrg} managers for {org.Name}",
                    CurrentItem = managerProgress,
                    TotalItems = totalManagers,
                    PercentComplete = CalculateOverallProgress(4, 6, (double)managerProgress / totalManagers)
                });
            }

            await _repository.AddManagersAsync(allManagers, cancellationToken);
            await _repository.AddManagerDepartmentsAsync(managerDepartments, cancellationToken);

            // Stage 5: Create Users
            await progressCallback(new SeedProgress
            {
                Stage = "Users",
                Message = "Creating users...",
                CurrentItem = 0,
                TotalItems = totalUsers,
                PercentComplete = CalculateOverallProgress(5, 6, 0)
            });

            var allUsers = new List<OrganisationUser>();
            int userProgress = 0;
            const int userBatchSize = 500;

            foreach (var org in organisations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var orgDepts = departmentsByOrg[org.Id];
                var userBatch = new List<OrganisationUser>();

                for (int i = 0; i < usersPerOrg; i++)
                {
                    var dept = orgDepts[_random.Next(orgDepts.Count)];
                    var user = new OrganisationUser
                    {
                        Id = Guid.NewGuid(),
                        OrganisationId = org.Id,
                        DepartmentId = dept.Id
                    };
                    userBatch.Add(user);
                    allUsers.Add(user);
                    userProgress++;

                    if (userBatch.Count >= userBatchSize)
                    {
                        await _repository.AddUsersAsync(userBatch, cancellationToken);
                        userBatch.Clear();

                        await progressCallback(new SeedProgress
                        {
                            Stage = "Users",
                            Message = $"Created {userProgress} of {totalUsers} users",
                            CurrentItem = userProgress,
                            TotalItems = totalUsers,
                            PercentComplete = CalculateOverallProgress(5, 6, (double)userProgress / totalUsers)
                        });
                    }
                }

                if (userBatch.Count > 0)
                {
                    await _repository.AddUsersAsync(userBatch, cancellationToken);
                    await progressCallback(new SeedProgress
                    {
                        Stage = "Users",
                        Message = $"Created {userProgress} of {totalUsers} users",
                        CurrentItem = userProgress,
                        TotalItems = totalUsers,
                        PercentComplete = CalculateOverallProgress(5, 6, (double)userProgress / totalUsers)
                    });
                }
            }

            // Stage 6: Create Audit Records
            await progressCallback(new SeedProgress
            {
                Stage = "Audit Records",
                Message = "Creating audit records...",
                CurrentItem = 0,
                TotalItems = estimatedAuditRecords,
                PercentComplete = CalculateOverallProgress(6, 6, 0)
            });

            int auditProgress = 0;
            int totalAuditRecords = 0;
            const int auditBatchSize = 5000;
            var auditBatch = new List<AuditRecord>();

            foreach (var user in allUsers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var recordCount = _random.Next(minAuditRecords, maxAuditRecords + 1);
                var dates = GenerateRandomDates(recordCount);
                bool currentStatus = _random.Next(2) == 0;

                foreach (var date in dates)
                {
                    var record = new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Date = date,
                        Status = currentStatus ? "Valid" : "Invalid"
                    };
                    auditBatch.Add(record);
                    totalAuditRecords++;
                    currentStatus = !currentStatus;
                }

                auditProgress++;

                if (auditBatch.Count >= auditBatchSize)
                {
                    await _repository.AddAuditRecordsAsync(auditBatch, cancellationToken);
                    auditBatch.Clear();

                    await progressCallback(new SeedProgress
                    {
                        Stage = "Audit Records",
                        Message = $"Created audit records for {auditProgress} of {totalUsers} users ({totalAuditRecords} records)",
                        CurrentItem = totalAuditRecords,
                        TotalItems = estimatedAuditRecords,
                        PercentComplete = CalculateOverallProgress(6, 6, (double)auditProgress / totalUsers)
                    });
                }
            }

            if (auditBatch.Count > 0)
            {
                await _repository.AddAuditRecordsAsync(auditBatch, cancellationToken);
            }

            await progressCallback(new SeedProgress
            {
                Stage = "Complete",
                Message = $"Seeding complete! Created {totalOrgs} organisations, {totalDepartments} departments, {totalManagers} managers, {totalUsers} users, and {totalAuditRecords} audit records.",
                CurrentItem = totalAuditRecords,
                TotalItems = totalAuditRecords,
                PercentComplete = 100,
                IsComplete = true
            });
        }
        catch (OperationCanceledException)
        {
            await progressCallback(new SeedProgress
            {
                Stage = "Cancelled",
                Message = "Seeding was cancelled",
                IsComplete = true,
                HasError = true,
                ErrorMessage = "Operation cancelled by user"
            });
            throw;
        }
        catch (Exception ex)
        {
            await progressCallback(new SeedProgress
            {
                Stage = "Error",
                Message = "An error occurred during seeding",
                IsComplete = true,
                HasError = true,
                ErrorMessage = ex.Message
            });
            throw;
        }
    }

    public async Task ClearDataAsync(Func<SeedProgress, Task> progressCallback, CancellationToken cancellationToken = default)
    {
        await progressCallback(new SeedProgress
        {
            Stage = "Clearing",
            Message = "Clearing existing data...",
            PercentComplete = 0
        });

        await _repository.ClearAllDataAsync(cancellationToken);

        await progressCallback(new SeedProgress
        {
            Stage = "Complete",
            Message = "All data cleared successfully",
            PercentComplete = 100,
            IsComplete = true
        });
    }

    private List<int> GenerateRandomDates(int count)
    {
        var dates = new HashSet<int>();
        while (dates.Count < count)
        {
            var year = _random.Next(2022, 2027);
            var month = _random.Next(1, 13);
            var maxDay = DateTime.DaysInMonth(year, month);
            var day = _random.Next(1, maxDay + 1);
            dates.Add(year * 10000 + month * 100 + day);
        }
        return dates.OrderBy(d => d).ToList();
    }

    private static double CalculateOverallProgress(int currentStage, int totalStages, double stageProgress)
    {
        var stageWeight = 100.0 / totalStages;
        var completedStages = (currentStage - 1) * stageWeight;
        return completedStages + (stageProgress * stageWeight);
    }
}
