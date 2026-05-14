import { defineCollection, z } from 'astro:content';

const docs = defineCollection({
  type: 'content',
  schema: z.object({
    title: z.string(),
    order: z.number().default(100)
  })
});

export const collections = { docs };
