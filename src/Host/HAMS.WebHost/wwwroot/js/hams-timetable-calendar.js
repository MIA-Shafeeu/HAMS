// Whole-school timetable calendar (System Administration -> Teaching Assignments & Timetable).
// FullCalendar renders one static representative week: every event is a DayOfWeek-recurring slot
// (no calendar date), matching TimetableEntry's own shape, so paging next/previous week would just
// show byte-identical data - the toolbar is deliberately reduced to nothing.

const DAY_NAMES = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

document.addEventListener("DOMContentLoaded", () => {
    const calendarEl = document.getElementById("timetableCalendar");
    if (!calendarEl) return;

    initTimetableCalendar(calendarEl);
    initClassFilter();
});

async function initTimetableCalendar(calendarEl) {
    const eventsUrl = calendarEl.dataset.eventsUrl;
    const workingDaysUrl = calendarEl.dataset.workingDaysUrl;

    let hiddenDays = [];
    try {
        const response = await fetch(workingDaysUrl, { headers: { Accept: "application/json" } });
        if (response.ok) {
            const workingDays = await response.json();
            hiddenDays = [0, 1, 2, 3, 4, 5, 6].filter((d) => !workingDays.includes(d));
        }
    } catch {
        // If this fails, the calendar just shows every day - a display-only degradation, not a
        // data-correctness issue (ScheduleAsync still enforces the real working-day rule server-side).
    }

    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: "timeGridWeek",
        headerToolbar: false,
        height: "auto",
        firstDay: 0,
        hiddenDays,
        slotMinTime: "06:00:00",
        slotMaxTime: "20:00:00",
        slotDuration: "00:15:00",
        dayHeaderFormat: { weekday: "long" },
        nowIndicator: false,
        events: eventsUrl,
        eventDidMount(info) {
            const classId = info.event.extendedProps.classId;
            info.el.dataset.classId = classId;
            const checkbox = document.querySelector(`.hams-timetable-class-filter[value="${classId}"]`);
            if (checkbox && !checkbox.checked) {
                info.el.style.display = "none";
            }
        },
        dateClick(info) {
            if (info.jsEvent.detail !== 2) return; // only a real double-click opens the create modal
            openCreateModal(info.date);
        },
        eventClick(info) {
            openManageModal(info.event.extendedProps);
        },
    });
    calendar.render();
}

function openCreateModal(date) {
    document.getElementById("timetableEntryModalTitle").textContent = "Schedule a class";
    document.getElementById("createEntryForm").classList.remove("d-none");
    document.getElementById("manageEntryForm").classList.add("d-none");
    document.getElementById("createEntrySubmit").classList.remove("d-none");
    document.getElementById("manageEntrySubmit").classList.add("d-none");

    document.getElementById("createEntryDayOfWeek").value = DAY_NAMES[date.getDay()];
    document.getElementById("createEntryStartTime").value = formatTime(date);
    document.getElementById("createEntryEndTime").value = formatTime(addMinutes(date, 40));

    const classSelect = document.getElementById("createEntryClassId");
    classSelect.value = "";
    classSelect.dispatchEvent(new Event("change"));

    showModal();
}

function openManageModal(entry) {
    document.getElementById("timetableEntryModalTitle").textContent = "Manage entry";
    document.getElementById("createEntryForm").classList.add("d-none");
    document.getElementById("manageEntryForm").classList.remove("d-none");
    document.getElementById("createEntrySubmit").classList.add("d-none");
    document.getElementById("manageEntrySubmit").classList.remove("d-none");

    document.getElementById("manageEntryId").value = entry.timetableEntryId;
    document.getElementById("manageEntryClass").textContent = entry.className;
    document.getElementById("manageEntrySubject").textContent = entry.subjectName;
    document.getElementById("manageEntryTeacher").textContent = entry.teacherName;
    document.getElementById("manageEntryWhen").textContent =
        `${DAY_NAMES[entry.dayOfWeek]}, ${entry.startTime}–${entry.endTime}`;

    showModal();
}

function showModal() {
    const modalEl = document.getElementById("timetableEntryModal");
    (bootstrap.Modal.getOrCreateInstance(modalEl)).show();
}

function formatTime(date) {
    return `${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
}

function addMinutes(date, minutes) {
    return new Date(date.getTime() + minutes * 60000);
}

function initClassFilter() {
    document.querySelectorAll(".hams-timetable-class-filter").forEach((checkbox) => {
        checkbox.addEventListener("change", applyClassFilter);
    });
    applyClassFilter();
}

function applyClassFilter() {
    const hiddenClassIds = new Set(
        [...document.querySelectorAll(".hams-timetable-class-filter:not(:checked)")].map((cb) => cb.value));
    document.querySelectorAll("#timetableCalendar [data-class-id]").forEach((el) => {
        el.style.display = hiddenClassIds.has(el.dataset.classId) ? "none" : "";
    });
}
