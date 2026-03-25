import { parseOpenCliDocument } from "../data/openCli";

describe("OpenCLI compatibility parsing", () => {
  it("normalizes synthesized array noise and preserves single examples", () => {
    const json = JSON.stringify({
      opencli: "0.1-draft",
      info: {
        title: "demo",
        version: "1.0",
      },
      "x-inspectra": {
        synthesized: true,
        artifactSource: "synthesized-from-xmldoc",
      },
      options: null,
      commands: [
        {
          name: "alpha",
          aliases: "-a",
          options: null,
          arguments: null,
          commands: null,
          examples: "alpha run",
          metadata: null,
        },
      ],
      examples: null,
    });

    const document = parseOpenCliDocument(json);

    expect(document.options).toEqual([]);
    expect(document.examples).toEqual([]);
    expect(document.commands[0]?.aliases).toEqual(["-a"]);
    expect(document.commands[0]?.options).toEqual([]);
    expect(document.commands[0]?.arguments).toEqual([]);
    expect(document.commands[0]?.commands).toEqual([]);
    expect(document.commands[0]?.examples).toEqual(["alpha run"]);
    expect(document.commands[0]?.metadata).toEqual([]);
  });
});
