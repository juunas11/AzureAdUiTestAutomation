/// <reference types="cypress" />

context("fetchData", () => {
  beforeEach(function () {
    cy.login().get("[data-cy=nav-fetch-data]").click();
  });

  it("shows a table", function () {
    cy.get("[data-cy=forecast-table]").should("be.visible");
  });

  it("renders 5 items in table", function () {
    cy.get("[data-cy=forecast-table]")
      .find("[data-cy=forecast-row]")
      .should("have.length", 5);
  });
});
