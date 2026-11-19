using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Evaluation;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Institutions;
using FeriaAcademica.Domain.Notifications;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Users;
using FeriaAcademica.Domain.Works;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FeriaAcademica.Infrastructure.Persistence;

/// <summary>
/// Fills an empty database with a believable demo: students, teachers, a closed fair with works already evaluated by some people and
/// pending for others, an open fair with works in every review state, and a fair still being configured. Everything goes through the
/// domain methods, so the data obeys the same rules as the real flows. Only runs when "Demo:Seed" is true, and only once: it does
/// nothing if the demo institution already exists. Every demo user gets the password in "Demo:Password" (or <see cref="DefaultPassword"/>).
/// </summary>
public static class DemoDataSeeder
{
    public const string DefaultPassword = "Demo#2026pass";
    public const string EmailDomain = "demo.feria.test";
    public const string InstitutionName = "Universidad Demo";

    public static async Task SeedDemoDataAsync(this IServiceProvider services, IConfiguration config)
    {
        if (!config.GetValue<bool>("Demo:Seed")) return;

        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DemoDataSeeder));
        var db = sp.GetRequiredService<AppDbContext>();

        if (await db.Institutions.AnyAsync(i => i.Name == InstitutionName))
        {
            logger.LogInformation("The demo data is already there; nothing to do.");
            return;
        }

        var password = config["Demo:Password"];
        if (string.IsNullOrEmpty(password)) password = DefaultPassword;

        await new Builder(db, sp.GetRequiredService<UserManager<ApplicationUser>>(), sp.GetRequiredService<IFileStorage>(), password).RunAsync();
        logger.LogInformation("Demo data created. All demo users share the demo password; emails end in @{Domain}.", EmailDomain);
    }

    private sealed class Builder(AppDbContext db, UserManager<ApplicationUser> users, IFileStorage storage, string password)
    {
        private readonly DateTime _now = DateTime.UtcNow;
        private DateTime Ago(int days) => _now.AddDays(-days);
        private DateTime In(int days) => _now.AddDays(days);

        public async Task RunAsync()
        {
            // ---- People ----
            var admin = await db.Users.Where(u => u.IsSuperAdmin).Select(u => (Guid?)u.Id).FirstOrDefaultAsync();
            var marcela = await User("marcela.rios", "Marcela Ríos", "+57 300 555 0101");
            var carlos = await User("carlos.mendoza", "Carlos Mendoza", "+57 300 555 0102");
            var laura = await User("laura.gomez", "Laura Gómez", "+57 300 555 0103");
            var andres = await User("andres.pardo", "Andrés Pardo", "+57 300 555 0104");
            var sofia = await User("sofia.herrera", "Sofía Herrera", "+57 300 555 0105");

            var valentina = await User("valentina.torres", "Valentina Torres", "+57 311 555 0201", new DateOnly(2004, 3, 14));
            var santiago = await User("santiago.ramirez", "Santiago Ramírez", "+57 311 555 0202", new DateOnly(2003, 11, 2));
            var camila = await User("camila.ortiz", "Camila Ortiz", "+57 311 555 0203", new DateOnly(2004, 7, 21));
            var juan = await User("juan.lopez", "Juan David López", "+57 311 555 0204", new DateOnly(2003, 1, 30));
            var mariana = await User("mariana.castro", "Mariana Castro", "+57 311 555 0205", new DateOnly(2004, 9, 8));
            var daniel = await User("daniel.vargas", "Daniel Vargas", "+57 311 555 0206", new DateOnly(2002, 5, 17));
            var isabela = await User("isabela.moreno", "Isabela Moreno", "+57 311 555 0207", new DateOnly(2007, 12, 5), "Rosa Moreno", $"rosa.moreno@{EmailDomain}"); // a minor, with guardian
            var samuel = await User("samuel.rojas", "Samuel Rojas", "+57 311 555 0208", new DateOnly(2003, 6, 26));

            var paola = await User("paola.jimenez", "Paola Jiménez", "+57 320 555 0301");
            var esteban = await User("esteban.cruz", "Esteban Cruz", "+57 320 555 0302");
            var natalia = await User("natalia.duarte", "Natalia Duarte", "+57 320 555 0303");

            var createdBy = admin ?? marcela.Id;

            // ---- Institutions ----
            var udemo = Institution.Create(InstitutionName, "UDEMO");
            var norte = Institution.Create("Instituto Tecnológico del Norte", "ITN");
            db.Institutions.AddRange(udemo, norte);
            foreach (var teacher in new[] { carlos, laura }) db.InstitutionMemberships.Add(InstitutionMembership.Create(teacher.Id, udemo.Id, MembershipPosition.Teacher, canCreateEvents: true));
            db.InstitutionMemberships.Add(InstitutionMembership.Create(marcela.Id, udemo.Id, MembershipPosition.Administrative, canCreateEvents: true));
            foreach (var student in new[] { valentina, santiago, camila, juan, mariana, daniel, isabela, samuel })
                db.InstitutionMemberships.Add(InstitutionMembership.Create(student.Id, udemo.Id, MembershipPosition.Student));
            await db.SaveChangesAsync();

            // ---- 1. Closed fair: works already admitted, evaluation open to those who attended ----
            var science = AcademicEvent.Create(udemo.Id, "Feria de Ciencias 2026-I", createdBy, capacity: 120);
            science.ScheduleStage(StageType.Call, Ago(210), Ago(180));
            science.ScheduleStage(StageType.Reception, Ago(180), Ago(130));
            science.ScheduleStage(StageType.Admission, Ago(130), Ago(105));
            science.ScheduleStage(StageType.Registration, Ago(110), Ago(80));
            science.ScheduleStage(StageType.Evaluation, Ago(75), Ago(60));
            science.ScheduleStage(StageType.Results, Ago(60), Ago(50));
            science.Publish();
            science.Close();
            db.Events.Add(science);
            await db.SaveChangesAsync();

            GiveRole(science, marcela, EventRole.Coordinator);
            GiveRole(science, carlos, EventRole.Teacher);
            GiveRole(science, laura, EventRole.Teacher);
            GiveRole(science, andres, EventRole.Jury);
            GiveRole(science, sofia, EventRole.Jury);

            var deadline = science.SubmissionDeadline;
            var pValentina = Enroll(science, valentina, carlos, Ago(200));
            var pSantiago = Enroll(science, santiago, laura, Ago(200));
            var pCamila = Enroll(science, camila, carlos, Ago(199));
            var pJuan = Enroll(science, juan, laura, Ago(199));
            var pMariana = Enroll(science, mariana, carlos, Ago(198));
            var pDaniel = Enroll(science, daniel, laura, Ago(198));

            var wValentina = await NewWork(science, pValentina, carlos, valentina, "Biofiltros con cáscara de plátano para remover metales pesados",
                "Un filtro de bajo costo hecho con residuos de cosecha que retiene plomo y cadmio del agua de quebradas.", Ago(170));
            Submit(wValentina, valentina, carlos, Ago(160), deadline);
            wValentina.Admit(carlos.Id, Ago(150), "Metodología sólida y resultados reproducibles.");
            await FinalVersion(wValentina, Ago(100));

            var wSantiago = await NewWork(science, pSantiago, laura, santiago, "Predicción de deslizamientos con redes neuronales y datos satelitales",
                "Modelo que combina lluvia, pendiente y cobertura vegetal para alertar zonas de riesgo en laderas.", Ago(168), video: "https://www.youtube.com/watch?v=demo-deslizamientos");
            Submit(wSantiago, santiago, laura, Ago(158), deadline);
            wSantiago.Admit(laura.Id, Ago(148), "Muy buen uso de datos abiertos.");
            await FinalVersion(wSantiago, Ago(98));

            // Approved with changes once (back to draft), corrected, then admitted: the history shows two attempts.
            var wCamila = await NewWork(science, pCamila, carlos, camila, "Impacto del microplástico en cuencas urbanas",
                "Muestreo en cinco puntos del río para medir la concentración de microplásticos en época de lluvia y de sequía.", Ago(166));
            Submit(wCamila, camila, carlos, Ago(156), deadline);
            wCamila.Decide(carlos.Id, ReviewDecision.ApprovedWithChanges, "Falta describir el protocolo de muestreo y la cantidad de réplicas.", Ago(152));
            await AddDocument(wCamila, "protocolo-de-muestreo.pdf", "Documento corregido", Ago(150), deadline);
            wCamila.Submit(camila.Id, Ago(149), deadline);
            wCamila.StartReview(carlos.Id, Ago(147));
            wCamila.Admit(carlos.Id, Ago(146), "Ahora sí queda claro el protocolo.");

            var wJuan = await NewWork(science, pJuan, laura, juan, "Prototipo de purificador solar de agua",
                "Destilador solar de dos etapas construido con materiales reciclados, probado en una vereda sin acueducto.", Ago(165), video: "https://www.youtube.com/watch?v=demo-purificador");
            Submit(wJuan, juan, laura, Ago(155), deadline);
            wJuan.Admit(laura.Id, Ago(145), "Gran impacto social.");
            await FinalVersion(wJuan, Ago(97));

            var wMariana = await NewWork(science, pMariana, carlos, mariana, "Compostaje con lombrices en escuelas rurales",
                "Guía y piloto de lombricultura para convertir los residuos del restaurante escolar en abono.", Ago(164));
            Submit(wMariana, mariana, carlos, Ago(154), deadline);
            wMariana.Admit(carlos.Id, Ago(144));

            var wDaniel = await NewWork(science, pDaniel, laura, daniel, "Generador eólico de bajo costo",
                "Aerogenerador casero para cargar baterías.", Ago(163));
            Submit(wDaniel, daniel, laura, Ago(153), deadline);
            wDaniel.Decide(laura.Id, ReviewDecision.Rejected, "El trabajo no incluye mediciones ni análisis; no cumple el alcance de la feria.", Ago(143));

            db.Works.AddRange(wValentina, wSantiago, wCamila, wJuan, wMariana, wDaniel);
            foreach (var work in new[] { wValentina, wSantiago, wJuan })
            {
                db.JuryAssignments.Add(JuryAssignment.Create(work.Id, andres.Id));
                db.JuryAssignments.Add(JuryAssignment.Create(work.Id, sofia.Id));
            }
            db.JuryAssignments.Add(JuryAssignment.Create(wCamila.Id, andres.Id));
            db.JuryAssignments.Add(JuryAssignment.Create(wMariana.Id, sofia.Id));

            // Guests and some students attended; those are the people who can evaluate the works.
            Attend(science, paola, laura, Ago(70), attended: true);
            Attend(science, esteban, laura, Ago(70), attended: true);
            Attend(science, natalia, carlos, Ago(70), attended: true);   // attended but has not evaluated yet: ready for the demo
            Register(science, santiago, laura, Ago(95), attended: true);
            Register(science, camila, carlos, Ago(95), attended: true);   // attended, no evaluation yet
            Register(science, isabela, carlos, Ago(95), attended: false); // registered, never came
            await db.SaveChangesAsync();

            var instrument = Instrument.Create(science.Id, InstrumentType.Survey, EvaluatorGroup.Attendees, "Evaluation of Feria de Ciencias 2026-I");
            instrument.ReplaceQuestions(Questions, hasAnswers: false, Ago(120));
            db.Instruments.Add(instrument);
            await db.SaveChangesAsync();

            var q = instrument.Criteria.OrderBy(c => c.Order).ToList();
            Evaluate(instrument, q, wValentina, paola, 5, 5, 4, true, "Una solución clara y barata. Me gustaría verla en mi municipio.");
            Evaluate(instrument, q, wSantiago, paola, 4, 4, 5, true, "Excelente uso de datos, aunque la presentación fue muy técnica.");
            Evaluate(instrument, q, wJuan, paola, 5, 4, 4, true, "Se nota que lo probaron en campo.");
            Evaluate(instrument, q, wValentina, esteban, 4, 5, 4, true, "Muy buena explicación.");
            Evaluate(instrument, q, wCamila, esteban, 3, 3, 4, false, "El tema es importante pero faltó mostrar más resultados.");
            Evaluate(instrument, q, wValentina, santiago, 5, 4, 5, true, "Rigurosa y bien presentada.");
            await db.SaveChangesAsync();

            Notify(valentina, science, "work.admitted", "Tu trabajo «Biofiltros con cáscara de plátano…» fue admitido.", Ago(150));
            Notify(daniel, science, "work.rejected", "Tu trabajo «Generador eólico de bajo costo» fue rechazado definitivamente.", Ago(143));
            Notify(natalia, science, "event.closed", "La Feria de Ciencias 2026-I terminó. Ya puedes evaluar los trabajos.", Ago(50));
            Notify(camila, science, "event.closed", "La Feria de Ciencias 2026-I terminó. Ya puedes evaluar los trabajos.", Ago(50));

            // ---- 2. Open fair: submissions are open and every review state is represented ----
            var tech = AcademicEvent.Create(udemo.Id, "Feria de Innovación y Tecnología 2026-II", createdBy, capacity: 80);
            tech.ScheduleStage(StageType.Call, Ago(45), Ago(30));
            tech.ScheduleStage(StageType.Reception, Ago(30), In(20));
            tech.ScheduleStage(StageType.Admission, In(20), In(30));
            tech.ScheduleStage(StageType.Registration, Ago(10), In(40));
            tech.ScheduleStage(StageType.Evaluation, In(45), In(55));
            tech.ScheduleStage(StageType.Results, In(55), In(60));
            tech.Publish();
            db.Events.Add(tech);
            await db.SaveChangesAsync();

            GiveRole(tech, marcela, EventRole.Coordinator);
            GiveRole(tech, carlos, EventRole.Teacher);
            GiveRole(tech, laura, EventRole.Teacher);
            GiveRole(tech, andres, EventRole.Jury);

            deadline = tech.SubmissionDeadline;
            var tValentina = Enroll(tech, valentina, carlos, Ago(25));
            var tSantiago = Enroll(tech, santiago, laura, Ago(25));
            var tIsabela = Enroll(tech, isabela, laura, Ago(24));
            var tSamuel = Enroll(tech, samuel, carlos, Ago(24));
            var tMariana = Enroll(tech, mariana, laura, Ago(23));
            var tCamila = Enroll(tech, camila, carlos, Ago(23));
            var tJuan = Enroll(tech, juan, laura, Ago(22));

            // Draft: the author is still working on it.
            var xValentina = await NewWork(tech, tValentina, carlos, valentina, "Asistente de voz para accesibilidad en laboratorios",
                "Control por voz de equipos de laboratorio para estudiantes con movilidad reducida.", Ago(3));

            // Submitted: waiting in the teacher's queue (log in as Laura to review it).
            var xSantiago = await NewWork(tech, tSantiago, laura, santiago, "Sistema de riego inteligente con IoT",
                "Sensores de humedad y una app que decide cuándo regar según el pronóstico del clima.", Ago(6), video: "https://www.youtube.com/watch?v=demo-riego");
            xSantiago.Submit(santiago.Id, Ago(2), deadline);   // sent only: nobody has opened it

            // In review: the teacher already opened it.
            var xIsabela = await NewWork(tech, tIsabela, laura, isabela, "App móvil para turnos en centros de salud",
                "Reduce las filas en puestos de salud permitiendo pedir turno desde el celular.", Ago(8));
            Submit(xIsabela, isabela, laura, Ago(4), deadline);   // sent, and the teacher opened it

            // Sent back once for changes, corrected and admitted.
            var xSamuel = await NewWork(tech, tSamuel, carlos, samuel, "Plataforma de monitoreo de calidad del aire",
                "Red de sensores de bajo costo con un tablero público de PM2.5 por barrio.", Ago(14));
            Submit(xSamuel, samuel, carlos, Ago(12), deadline);
            xSamuel.Decide(carlos.Id, ReviewDecision.ApprovedWithChanges, "Incluye la calibración de los sensores contra una estación de referencia.", Ago(10));
            await AddDocument(xSamuel, "calibracion-sensores.pdf", "Versión con calibración", Ago(8), deadline);
            xSamuel.Submit(samuel.Id, Ago(7), deadline);
            xSamuel.StartReview(carlos.Id, Ago(6));
            xSamuel.Admit(carlos.Id, Ago(5), "Aprobado, buen trabajo de calibración.");

            // Admitted and already with its final version.
            var xMariana = await NewWork(tech, tMariana, laura, mariana, "Gemelo digital de un invernadero",
                "Simulación en tiempo real de temperatura y humedad para optimizar el cultivo de tomate.", Ago(15), video: "https://www.youtube.com/watch?v=demo-gemelo");
            Submit(xMariana, mariana, laura, Ago(13), deadline);
            xMariana.Admit(laura.Id, Ago(9), "Muy completo.");
            await FinalVersion(xMariana, Ago(2));

            // Withdrawn by its author.
            var xCamila = await NewWork(tech, tCamila, carlos, camila, "Chatbot para orientación vocacional",
                "Asistente conversacional que sugiere carreras según intereses.", Ago(9));
            xCamila.Withdraw(camila.Id, Ago(5));

            // Rejected definitively.
            var xJuan = await NewWork(tech, tJuan, laura, juan, "Criptomoneda para pagos en la cafetería",
                "Una moneda propia para la cafetería universitaria.", Ago(10));
            Submit(xJuan, juan, laura, Ago(8), deadline);
            xJuan.Decide(laura.Id, ReviewDecision.Rejected, "No tiene un componente de investigación; queda fuera del alcance.", Ago(6));

            db.Works.AddRange(xValentina, xSantiago, xIsabela, xSamuel, xMariana, xCamila, xJuan);
            db.JuryAssignments.Add(JuryAssignment.Create(xSamuel.Id, andres.Id));
            db.JuryAssignments.Add(JuryAssignment.Create(xMariana.Id, andres.Id));

            Register(tech, paola, laura, Ago(5), attended: false);
            Register(tech, esteban, laura, Ago(4), attended: false);
            Register(tech, natalia, carlos, Ago(3), attended: false);
            Register(tech, santiago, laura, Ago(3), attended: false);

            // Invitations still waiting for an answer (the tokens are random: they are only there to fill the list).
            db.Invitations.Add(Invitation.Create(tech.Id, $"estudiante.nuevo@{EmailDomain}", EventRole.Participant, InvitationOrigin.Invitation, carlos.Id, Token(), Ago(1) + Invitation.Lifetime));
            db.Invitations.Add(Invitation.Create(tech.Id, $"invitado.especial@{EmailDomain}", EventRole.Attendee, InvitationOrigin.Invitation, laura.Id, Token(), Ago(1) + Invitation.Lifetime));

            var techInstrument = Instrument.Create(tech.Id, InstrumentType.Survey, EvaluatorGroup.Attendees, "Evaluation of Feria de Innovación y Tecnología 2026-II");
            techInstrument.ReplaceQuestions(Questions, hasAnswers: false, Ago(5));
            db.Instruments.Add(techInstrument);

            Notify(laura, tech, "work.submitted", "Santiago Ramírez envió «Sistema de riego inteligente con IoT» para revisión.", Ago(2));
            Notify(samuel, tech, "work.admitted", "Tu trabajo «Plataforma de monitoreo de calidad del aire» fue admitido.", Ago(5));
            Notify(mariana, tech, "work.admitted", "Tu trabajo «Gemelo digital de un invernadero» fue admitido.", Ago(9));
            Notify(juan, tech, "work.rejected", "Tu trabajo «Criptomoneda para pagos en la cafetería» fue rechazado definitivamente.", Ago(6));

            // ---- 3. Fair still being configured ----
            var arts = AcademicEvent.Create(udemo.Id, "Feria de Arte y Humanidades 2027", createdBy);
            arts.ScheduleStage(StageType.Call, In(90), In(120));
            arts.ScheduleStage(StageType.Reception, In(120), In(180));
            arts.ScheduleStage(StageType.Evaluation, In(200), In(210));
            db.Events.Add(arts);
            await db.SaveChangesAsync();
            GiveRole(arts, marcela, EventRole.Coordinator);
            GiveRole(arts, laura, EventRole.Teacher);

            await db.SaveChangesAsync();
        }

        // ---------------------------------------------------------------- helpers

        private static readonly QuestionDraft[] Questions =
        [
            new(null, CriterionType.Scale, "¿Qué tan original es el trabajo?", 1, 5),
            new(null, CriterionType.Scale, "¿Qué tan clara fue la presentación?", 1, 5),
            new(null, CriterionType.Scale, "¿Qué tan riguroso es el trabajo?", 1, 5),
            new(null, CriterionType.YesNo, "¿Recomendarías este trabajo para la siguiente feria?", null, null),
            new(null, CriterionType.Text, "Comentarios para los autores", null, null),
        ];

        private async Task<ApplicationUser> User(string local, string fullName, string phone, DateOnly? birth = null, string? guardian = null, string? guardianEmail = null)
        {
            var email = $"{local}@{EmailDomain}";
            if (await users.FindByEmailAsync(email) is { } existing) return existing;

            var user = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true, FullName = fullName, PhoneNumber = phone,
                BirthDate = birth, GuardianName = guardian, GuardianEmail = guardianEmail, Status = UserStatus.Active,
            };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException($"Could not create demo user {email}: " + string.Join(" ", result.Errors.Select(e => e.Description)));
            return user;
        }

        private void GiveRole(AcademicEvent ev, ApplicationUser user, EventRole role) => db.RoleAssignments.Add(RoleAssignment.Create(user.Id, ev.Id, role));

        /// <summary>A teacher invited the student, who accepted a day later: they get the Participant role and a participation linked to that invitation.</summary>
        private Participation Enroll(AcademicEvent ev, ApplicationUser student, ApplicationUser teacher, DateTime sentAt)
        {
            var invitation = Invitation.Create(ev.Id, student.Email!, EventRole.Participant, InvitationOrigin.Invitation, teacher.Id, Token(), sentAt + Invitation.Lifetime);
            var role = invitation.Accept(student.Id, student.Email!, sentAt.AddDays(1));
            var participation = Participation.Create(ev.Id, student.Id, invitation.Id);
            db.Invitations.Add(invitation);
            db.RoleAssignments.Add(role);
            db.Participations.Add(participation);
            return participation;
        }

        /// <summary>An invited guest (Attendee) who registers for the fair, and optionally gets scanned at the door.</summary>
        private void Attend(AcademicEvent ev, ApplicationUser guest, ApplicationUser teacher, DateTime at, bool attended)
        {
            var invitation = Invitation.Create(ev.Id, guest.Email!, EventRole.Attendee, InvitationOrigin.Invitation, teacher.Id, Token(), at.AddDays(-10) + Invitation.Lifetime);
            db.RoleAssignments.Add(invitation.Accept(guest.Id, guest.Email!, at.AddDays(-9)));
            db.Invitations.Add(invitation);
            Register(ev, guest, teacher, at, attended);
        }

        private void Register(AcademicEvent ev, ApplicationUser person, ApplicationUser scannedBy, DateTime at, bool attended)
        {
            var registration = Registration.Create(ev.Id, person.Id, Token());
            db.Registrations.Add(registration);
            if (!attended) return;
            var attendance = registration.CheckIn(null, scannedBy.Id, at).Attendance;
            if (attendance is not null) db.Attendances.Add(attendance);
        }

        /// <summary>A draft work with its document (a real, tiny PDF, so it can be downloaded) and optionally a video link.</summary>
        private async Task<Work> NewWork(AcademicEvent ev, Participation participation, ApplicationUser reviewer, ApplicationUser author, string title, string summary, DateTime at, string? video = null)
        {
            var work = Work.Create(participation.Id, reviewer.Id, title, summary, ev.MaxRejections, author.Id, at, ev.SubmissionDeadline);
            await AddDocument(work, "documento.pdf", title, at.AddHours(1), ev.SubmissionDeadline);
            if (video is not null) work.AddArtifact(ArtifactType.Video, null, video, at.AddHours(2), ev.SubmissionDeadline);
            return work;
        }

        /// <summary>The author sends it and the teacher opens it the next day, so it is ready for a decision.</summary>
        private static void Submit(Work work, ApplicationUser author, ApplicationUser reviewer, DateTime at, DateTime? deadline)
        {
            work.Submit(author.Id, at, deadline);
            work.StartReview(reviewer.Id, at.AddDays(1));
        }

        private async Task AddDocument(Work work, string fileName, string heading, DateTime at, DateTime? deadline)
        {
            var bytes = DemoPdf(heading);
            var fileId = Guid.NewGuid();
            work.AddFile(ArtifactType.Document, FileUrl(work.Id, fileId), fileName, bytes.Length, at, deadline);
            await Store(work.Id, fileId, bytes);
        }

        private async Task FinalVersion(Work work, DateTime at)
        {
            var bytes = DemoPdf("Versión final");
            var fileId = Guid.NewGuid();
            work.SubmitFinalVersion(FileUrl(work.Id, fileId), "version-final.pdf", bytes.Length, at);
            await Store(work.Id, fileId, bytes);
        }

        // Same location the upload endpoint uses (WorkFileLocation is internal to Application).
        private static string FileUrl(Guid workId, Guid fileId) => $"/api/works/{workId}/files/{fileId}";

        private async Task Store(Guid workId, Guid fileId, byte[] bytes)
        {
            await using var content = new MemoryStream(bytes);
            await storage.SaveAsync($"{workId}/{fileId}.pdf", content, CancellationToken.None);
        }

        private void Evaluate(Instrument instrument, List<Criterion> q, Work work, ApplicationUser evaluator, int originality, int clarity, int rigor, bool recommend, string comment)
        {
            var answers = new List<AnswerDraft>
            {
                new(q[0].Id, originality, null), new(q[1].Id, clarity, null), new(q[2].Id, rigor, null),
                new(q[3].Id, recommend ? 1 : 0, null), new(q[4].Id, null, comment),
            };
            db.Responses.AddRange(instrument.BuildResponses(work.Id, evaluator.Id, answers, _now.AddDays(-65)));
        }

        private void Notify(ApplicationUser user, AcademicEvent ev, string type, string message, DateTime at) =>
            db.Notifications.Add(Notification.Create(user.Id, ev.Id, type, message, at));

        private static string Token() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        /// <summary>A one-page PDF written by hand (no fonts or libraries), enough for the demo to open and download.</summary>
        private static byte[] DemoPdf(string heading)
        {
            static string Ascii(string text)
            {
                var plain = new StringBuilder();
                foreach (var ch in text.Normalize(NormalizationForm.FormD))
                    if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark && ch is >= ' ' and <= '~') plain.Append(ch);
                return plain.ToString().Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            }

            var stream = $"BT /F1 20 Tf 72 760 Td ({Ascii(heading)}) Tj 0 -32 Td /F1 12 Tf (Documento de demostracion - Feria Academica) Tj ET";
            string[] objects =
            [
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
                $"<< /Length {stream.Length} >>\nstream\n{stream}\nendstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            ];

            var pdf = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int>();
            for (var i = 0; i < objects.Length; i++)
            {
                offsets.Add(pdf.Length);
                pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }
            var xref = pdf.Length;
            pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets) pdf.Append($"{offset:D10} 00000 n \n");
            pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return Encoding.ASCII.GetBytes(pdf.ToString());
        }
    }
}
