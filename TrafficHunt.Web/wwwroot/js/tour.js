// Guided tour of the TrafficHunt operator console (intro.js, loaded locally).
(function () {
    'use strict';

    var page = document.body.getAttribute('data-page') || '';

    // Steps are filtered to what exists on the current page, so the same
    // script works across the whole console without per-view tweaks.
    var TOURS = {
        // Full tour — only sensible on the dashboard, walks the whole console.
        '/': [
            { el: '.sidebar', intro: 'This is the operator console sidebar — every section of TrafficHunt lives here. You never leave this UI; there is no separate app.' },
            { el: '.stat-cards, .grid.cards', intro: 'Your global pipeline at a glance: total prospects, high-intent leads, and outreach progress across all campaigns.', position: 'right' },
            { el: 'a[href="/Campaigns"]', intro: '<b>Campaigns</b> — what you are promoting. Each campaign holds your product info, discovery keywords and AI instructions.', position: 'right' },
            { el: 'a[href="/Prospects"]', intro: '<b>Prospects</b> — people the AI found and qualified from YouTube comments, scored by intent.', position: 'right' },
            { el: 'a[href="/ReplyCampaigns"]', intro: '<b>Reply Campaigns</b> — sequences that deliver your replies to prospects on a human-approved schedule.', position: 'right' },
            { el: 'a[href="/Jobs"]', intro: '<b>Background Jobs</b> — discovery, comment import and AI analysis run as Hangfire jobs. Watch progress here.', position: 'right' },
            { el: '.btn-row', intro: 'Ready to start? Create a campaign manually, or let the AI draft one from a plain-English description.', position: 'bottom' }
        ],
        Campaigns: [
            { el: '.btn-row', intro: 'Create a campaign manually, or use <b>AI Plan from Description</b> — describe your product in plain English and the AI fills in keywords, problems and qualification rules.' },
            { el: '.table', intro: 'Each row is a campaign: its discovery keywords, prospects found, and how many scored as high intent. Use <b>Open</b> to dig in, <b>Edit</b> to change it, or <b>Delete</b> to remove it (cascades to its prospects).', position: 'bottom' }
        ],
        CampaignsCreate: [
            { el: 'form', intro: 'Fill in what you are promoting and who it is for. The more detail you give the AI, the better its qualification and reply drafts will be.', position: 'bottom' }
        ],
        CampaignsPlan: [
            { el: 'form, .card', intro: 'Paste a plain-English description of your product. The AI drafts the full campaign: name, value proposition, target audience, discovery keywords and problems solved.', position: 'bottom' }
        ],
        CampaignsDetails: [
            { el: '.grid.cards', intro: 'Live stats for this campaign: prospects found, high-intent count, contacted and converted.', position: 'bottom' },
            { el: '.card', intro: 'On the left, the campaign configuration. Discovery keywords (right) drive the YouTube search — add or remove them any time, then hit <b>Run Discovery</b> to enqueue a background job.', position: 'right' }
        ],
        Prospects: [
            { el: 'table', intro: 'Every prospect the AI qualified from YouTube comments, with an intent score. Filter by campaign or status — only the high scorers are worth your outreach.', position: 'bottom' }
        ],
        ReplyCampaigns: [
            { el: '.btn-row', intro: 'Reply campaigns deliver your outreach to prospects with human-in-the-loop control: you approve the templates and the timing window, the system handles rotation and delays.', position: 'bottom' },
            { el: 'table', intro: 'Track sent / failed / pending counts per campaign. Start or pause them from the detail page.', position: 'bottom' }
        ],
        Jobs: [
            { el: '.grid, table', intro: 'The job pipeline: Discover → Import Comments → Analyze (AI) → Detect Opportunities. Queue separation keeps YouTube fetching fast while the AI bottleneck stays small.', position: 'bottom' }
        ]
    };

    function key() {
        if (page === 'Home') return '/';
        var action = document.body.getAttribute('data-action') || '';
        if (page === 'Campaigns' && (action === 'Create' || action === 'Edit')) return 'CampaignsCreate';
        if (page === 'Campaigns' && action === 'Plan') return 'CampaignsPlan';
        return page;
    }

    function stepsFor() {
        var steps = TOURS[key()] || [];
        return steps.filter(function (s) { return document.querySelector(s.el); });
    }

    function startTour() {
        var steps = stepsFor();
        if (!steps.length || typeof introJs === 'undefined') return;
        introJs().setOptions({
            steps: steps,
            showStepNumbers: false,
            nextLabel: 'Next &rarr;',
            prevLabel: '&larr; Back',
            doneLabel: 'Got it',
            skipLabel: '&times;',
            tooltipClass: 'th-tour'
        }).start();
    }

    // "Tour" button in the topbar + auto-start once per browser.
    document.addEventListener('DOMContentLoaded', function () {
        var btn = document.getElementById('startTour');
        if (btn) btn.addEventListener('click', startTour);

        try {
            if (page === 'Home' && !localStorage.getItem('th_tour_done')) {
                setTimeout(function () {
                    startTour();
                    localStorage.setItem('th_tour_done', '1');
                }, 600);
            }
        } catch (e) { /* storage disabled — no auto-start */ }
    });
})();
