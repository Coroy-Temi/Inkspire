document.addEventListener("DOMContentLoaded", () => {
    const pageBreakToken = "[[page-break]]";
    const themeStorageKey = "inkspire-theme";
    const themeToggle = document.querySelector("[data-theme-toggle]");
    const themeLabel = document.querySelector("[data-theme-label]");
    const themeState = document.querySelector("[data-theme-state]");
    const prefersDark = window.matchMedia ? window.matchMedia("(prefers-color-scheme: dark)") : null;

    const applyTheme = (theme) => {
        const nextTheme = theme === "dark" ? "dark" : "light";
        document.documentElement.dataset.theme = nextTheme;
        document.documentElement.style.colorScheme = nextTheme;

        if (themeToggle) {
            const darkModeActive = nextTheme === "dark";
            themeToggle.setAttribute("aria-pressed", String(darkModeActive));

            if (themeLabel) {
                themeLabel.textContent = "Dark mode";
            }

            if (themeState) {
                themeState.textContent = darkModeActive ? "On" : "Off";
            }
        }
    };

    const storedTheme = window.localStorage.getItem(themeStorageKey);
    applyTheme(storedTheme || (prefersDark?.matches ? "dark" : "light"));

    if (themeToggle) {
        themeToggle.addEventListener("click", () => {
            const currentTheme = document.documentElement.dataset.theme === "dark" ? "dark" : "light";
            const nextTheme = currentTheme === "dark" ? "light" : "dark";
            window.localStorage.setItem(themeStorageKey, nextTheme);
            applyTheme(nextTheme);
        });
    }

    prefersDark?.addEventListener?.("change", (event) => {
        if (!window.localStorage.getItem(themeStorageKey)) {
            applyTheme(event.matches ? "dark" : "light");
        }
    });

    const searchInput = document.querySelector("[data-library-search]");
    const cards = Array.from(document.querySelectorAll("[data-library-card]"));
    const emptyState = document.querySelector("[data-library-empty]");

    if (searchInput && cards.length) {
        const runFilter = () => {
            const term = searchInput.value.trim().toLowerCase();
            let visible = 0;

            cards.forEach((card) => {
                const haystack = (card.dataset.search || "").toLowerCase();
                const matches = !term || haystack.includes(term);
                card.classList.toggle("d-none", !matches);
                if (matches) {
                    visible += 1;
                }
            });

            if (emptyState) {
                emptyState.classList.toggle("d-none", visible !== 0);
            }
        };

        searchInput.addEventListener("input", runFilter);
        runFilter();
    }

    const accessTypeFields = document.querySelectorAll("[data-access-type]");
    accessTypeFields.forEach((select) => {
        const form = select.closest("form");
        if (!form) {
            return;
        }

        const panels = form.querySelectorAll("[data-access-panel]");
        const syncPanels = () => {
            panels.forEach((panel) => {
                panel.classList.toggle("d-none", panel.dataset.accessPanel !== select.value);
            });
        };

        select.addEventListener("change", syncPanels);
        syncPanels();
    });

    const writingInput = document.querySelector("[data-writing-input]");
    const previewRoot = document.querySelector("[data-writing-preview]");
    const previewButtons = Array.from(document.querySelectorAll("[data-preview-font]"));
    const pageBreakButton = document.querySelector("[data-insert-page-break]");
    let previewFont = "classic";

    const renderPreviewPages = () => {
        if (!writingInput || !previewRoot) {
            return;
        }

        const rawValue = writingInput.value.replace(/\r\n/g, "\n");
        const pages = rawValue
            .split(pageBreakToken)
            .map((page) => page.trim())
            .filter((page) => page.length > 0);

        previewRoot.innerHTML = "";

        if (!pages.length) {
            const placeholder = document.createElement("article");
            placeholder.className = `writing-preview writing-preview--${previewFont} writing-preview--empty`;
            placeholder.textContent = "Your poem or excerpt will appear here exactly with its line breaks so you can judge spacing and tone before publishing.";
            previewRoot.appendChild(placeholder);
            return;
        }

        pages.forEach((page, index) => {
            const card = document.createElement("article");
            card.className = `writing-preview writing-preview--${previewFont}`;

            const header = document.createElement("div");
            header.className = "writing-preview__header";
            header.textContent = `Page ${index + 1}`;

            const body = document.createElement("div");
            body.className = "writing-preview__body";
            body.textContent = page;

            card.append(header, body);
            previewRoot.appendChild(card);
        });
    };

    if (writingInput && previewRoot) {
        writingInput.addEventListener("input", renderPreviewPages);
        renderPreviewPages();
    }

    if (pageBreakButton && writingInput) {
        pageBreakButton.addEventListener("click", () => {
            const token = `\n\n${pageBreakToken}\n\n`;
            const start = writingInput.selectionStart ?? writingInput.value.length;
            const end = writingInput.selectionEnd ?? writingInput.value.length;
            const value = writingInput.value;
            writingInput.value = `${value.slice(0, start)}${token}${value.slice(end)}`;
            const nextCursor = start + token.length;
            writingInput.focus();
            writingInput.setSelectionRange(nextCursor, nextCursor);
            writingInput.dispatchEvent(new Event("input", { bubbles: true }));
        });
    }

    if (previewButtons.length && previewRoot) {
        previewButtons.forEach((button) => {
            button.addEventListener("click", () => {
                previewButtons.forEach((item) => item.classList.remove("active"));
                button.classList.add("active");
                previewFont = button.dataset.previewFont || "classic";
                renderPreviewPages();
            });
        });
    }

    const studioGuideModalElement = document.querySelector("[data-studio-guide-modal]");
    const studioGuideButtons = document.querySelectorAll("[data-studio-guide-open]");
    if (studioGuideModalElement && typeof bootstrap !== "undefined") {
        const storageKey = "inkspire-author-studio-guide-seen";
        const studioGuideModal = bootstrap.Modal.getOrCreateInstance(studioGuideModalElement);
        const hasSeenGuide = window.localStorage.getItem(storageKey) === "true";

        if (!hasSeenGuide) {
            studioGuideModal.show();
        }

        studioGuideModalElement.addEventListener("hidden.bs.modal", () => {
            window.localStorage.setItem(storageKey, "true");
        });

        studioGuideButtons.forEach((button) => {
            button.addEventListener("click", () => {
                studioGuideModal.show();
            });
        });
    }

    const likeForm = document.querySelector("[data-like-form]");
    const likeButton = document.querySelector("[data-like-button]");
    const likeCount = document.querySelector("[data-like-count]");
    if (likeForm && likeButton && likeCount) {
        likeForm.addEventListener("submit", async (event) => {
            event.preventDefault();

            const previousText = likeButton.textContent;
            likeButton.disabled = true;

            try {
                const response = await fetch(likeForm.action, {
                    method: "POST",
                    headers: {
                        "X-Requested-With": "XMLHttpRequest",
                        "Accept": "application/json"
                    },
                    body: new FormData(likeForm)
                });

                if (!response.ok) {
                    throw new Error("Like request failed");
                }

                const data = await response.json();
                likeButton.textContent = data.label;
                likeCount.textContent = data.count;
                likeButton.classList.toggle("btn-brand--ghost", !data.liked);
                likeButton.dataset.liked = String(data.liked);
            } catch {
                likeButton.textContent = previousText;
            } finally {
                likeButton.disabled = false;
            }
        });
    }

    const readerShell = document.querySelector("[data-reader-shell]");
    const readerBody = document.querySelector("[data-reader-body]");
    const readerFontButtons = Array.from(document.querySelectorAll("[data-reader-font]"));
    const readerWidthButtons = Array.from(document.querySelectorAll("[data-reader-width]"));
    const readerSize = document.querySelector("[data-reader-size]");
    const fullscreenButton = document.querySelector("[data-reader-fullscreen]");

    if (readerBody) {
        readerFontButtons.forEach((button) => {
            button.addEventListener("click", () => {
                readerFontButtons.forEach((item) => item.classList.remove("active"));
                button.classList.add("active");
                readerBody.classList.remove("poem-body--classic", "poem-body--book", "poem-body--modern");
                readerBody.classList.add(`poem-body--${button.dataset.readerFont}`);
            });
        });

        readerWidthButtons.forEach((button) => {
            button.addEventListener("click", () => {
                readerWidthButtons.forEach((item) => item.classList.remove("active"));
                button.classList.add("active");
                readerBody.classList.toggle("poem-body--wide", button.dataset.readerWidth === "wide");
            });
        });

        if (readerSize) {
            const syncSize = () => {
                readerBody.style.setProperty("--reader-font-size", `${readerSize.value}px`);
            };

            readerSize.addEventListener("input", syncSize);
            syncSize();
        }
    }

    if (readerShell && fullscreenButton) {
        const syncFullscreenLabel = () => {
            const active = document.fullscreenElement === readerShell || readerShell.classList.contains("poem-shell--fullscreen-fallback");
            fullscreenButton.textContent = active ? "Exit full screen" : "Full screen";
        };

        fullscreenButton.addEventListener("click", async () => {
            if (document.fullscreenEnabled && readerShell.requestFullscreen) {
                if (document.fullscreenElement === readerShell) {
                    await document.exitFullscreen();
                } else {
                    await readerShell.requestFullscreen();
                }
            } else {
                readerShell.classList.toggle("poem-shell--fullscreen-fallback");
                document.body.classList.toggle("reader-fullscreen-fallback");
            }

            syncFullscreenLabel();
        });

        document.addEventListener("fullscreenchange", syncFullscreenLabel);
    }
});
