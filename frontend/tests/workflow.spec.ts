import { test, expect } from "@playwright/test";
test("generate, edit, approve, move, record a test result and export", async ({
  page,
}) => {
  await page.goto("/");
  await page.getByRole("button", { name: "Fresh start", exact: true }).click();
  await page.getByRole("button", { name: "Step inside", exact: true }).click();
  await page.getByRole("button", { name: "Brief studio", exact: true }).click();
  const title = "Appointment flow " + Date.now();
  await page.getByLabel("Give your idea a name").fill(title);
  await page
    .getByLabel("Requirements", { exact: true })
    .fill(
      "Customers must be able to reserve an available provider slot without double booking.",
    );
  await page
    .getByRole("button", { name: "Generate a proposal", exact: false })
    .click();
  await expect(
    page.getByRole("button", { name: "Approve & add to board", exact: true }),
  ).toBeEnabled({ timeout: 20000 });
  await page
    .getByLabel("Story title", { exact: true })
    .fill(title + " reviewed");
  await page
    .getByRole("button", { name: "Approve & add to board", exact: true })
    .click();
  await page
    .getByRole("button", { name: title + " reviewed", exact: true })
    .click();
  await page
    .getByLabel("Story status", { exact: true })
    .selectOption("In progress");
  await page.getByLabel("Assignee", { exact: true }).fill("Test Reviewer");
  await page
    .getByLabel("Result for test 1", { exact: true })
    .selectOption("Passed");
  await page.getByRole("button", { name: "Save story", exact: true }).click();
  await expect(
    page.getByRole("button", { name: "Save story", exact: true }),
  ).toBeDisabled();
  await page
    .getByRole("button", { name: "Close inspector", exact: true })
    .click();
  const card = page.locator(".story-card").filter({
    has: page.getByRole("button", { name: title + " reviewed", exact: true }),
  });
  await expect(page.locator('[data-column="In progress"]')).toContainText(
    title + " reviewed",
  );
  await card.dragTo(page.locator('[data-column="Done"]'));
  await expect(page.locator('[data-column="Done"]')).toContainText(
    title + " reviewed",
  );
  await page.reload();
  await page
    .getByRole("button", { name: title + " reviewed", exact: true })
    .click();
  await expect(page.getByLabel("Story status", { exact: true })).toHaveValue(
    "Done",
  );
  await expect(
    page.getByLabel("Result for test 1", { exact: true }),
  ).toHaveValue("Passed");
  await page
    .getByRole("button", { name: "Close inspector", exact: true })
    .click();
  const download = page.waitForEvent("download");
  await page.getByRole("link", { name: "Markdown", exact: true }).click();
  expect((await download).suggestedFilename()).toBe("sprintpilot-backlog.md");
});
